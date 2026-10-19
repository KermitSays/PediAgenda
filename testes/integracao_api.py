"""Integração real HTTP + MySQL 8 em instâncias locais descartáveis.

Exige PEDIAGENDA_TESTE_DESTRUTIVO=SIM: apaga o schema pediagenda LOCAL.
Não aceita servidor externo. Não usa nem altera o banco do Aiven.
"""
import base64
import collections
import concurrent.futures
import datetime as dt
import hashlib
import hmac
import json
import os
from pathlib import Path
import secrets
import subprocess
import time
import urllib.error
import urllib.request
from zoneinfo import ZoneInfo

ROOT = Path(__file__).resolve().parents[1]
HOST = os.environ.get("MYSQL_HOST", "127.0.0.1")
PORT = int(os.environ.get("MYSQL_PORT", "3306"))
API_PORT = int(os.environ.get("PEDIAGENDA_TESTE_PORTA", "5099"))
BASE = f"http://127.0.0.1:{API_PORT}"
CHECKS = []
COUNTS = collections.Counter()
TOKEN_KEY = secrets.token_urlsafe(48)
HTTP = urllib.request.build_opener(urllib.request.ProxyHandler({}))


def sql(text):
    env = os.environ.copy()
    env["MYSQL_PWD"] = os.environ.get("MYSQL_PASSWORD", "")
    args = [os.environ.get("MYSQL_BIN", "mysql"), "--protocol=TCP", "--host=" + HOST,
            "--port=" + str(PORT), "--user=" + os.environ.get("MYSQL_USER", "root"),
            "--batch", "--skip-column-names", "--default-character-set=utf8mb4"]
    r = subprocess.run(args, input=text, text=True, capture_output=True, env=env, timeout=30)
    if r.returncode:
        raise AssertionError(r.stderr)
    return r.stdout.strip()


def request(method, path, expected, body=None, token=None, raw=None, label=None):
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        r = HTTP.open(req, timeout=30)
    except urllib.error.HTTPError as e:
        r = e
    with r:
        content = r.read()
        code = r.status
        content_type = r.headers.get("Content-Type", "")
    result = json.loads(content) if "json" in content_type and content else content.decode("utf-8-sig")
    assert code == expected, (method, path, expected, code, result)
    if code >= 400:
        assert isinstance(result, dict) and "codigo" in result and "mensagem" in result, result
    COUNTS[(method, path.split("?")[0])] += 1
    CHECKS.append({"metodo": method, "rota": path, "status": code, "cenario": label or ""})
    return result


def login(email, senha="senhaDemo123"):
    return request("POST", "/api/auth/login", 200, {"email": email, "senha": senha})["token"]


def jwt(payload):
    def enc(value):
        return base64.urlsafe_b64encode(json.dumps(value).encode()).rstrip(b"=")
    value = enc({"alg": "HS256", "typ": "JWT"}) + b"." + enc(payload)
    signature = base64.urlsafe_b64encode(hmac.new(TOKEN_KEY.encode(), value, hashlib.sha256).digest()).rstrip(b"=")
    return (value + b"." + signature).decode()


def main():
    if os.environ.get("PEDIAGENDA_TESTE_DESTRUTIVO") != "SIM" or HOST not in ("127.0.0.1", "localhost"):
        raise SystemExit("Use somente MySQL local descartável e PEDIAGENDA_TESTE_DESTRUTIVO=SIM.")
    for _ in range(100):
        try:
            sql("SELECT 1;")
            break
        except AssertionError:
            time.sleep(.2)
    else:
        raise AssertionError("MySQL local não iniciou.")
    sql("DROP DATABASE IF EXISTS pediagenda;")
    sql((ROOT / "banco/01_Schema.sql").read_text())
    sql((ROOT / "banco/02_popula_tabelas.sql").read_text())

    # Migração sobre schema atual e sobre as colunas legadas; repetir não pode apagar dados.
    preservados = "SELECT id_usuario,nome,cpf,email,HEX(senha_hash),HEX(senha_salt),ativo FROM pediagenda.usuario ORDER BY id_usuario;"
    before = sql(preservados)
    migration = (ROOT / "banco/03_remover_verificacao_email.sql").read_text()
    sql(migration)
    sql("""ALTER TABLE pediagenda.usuario ADD email_verificado BOOLEAN DEFAULT 0,
        ADD email_verificado_em DATETIME NULL, ADD token_verificacao_hash VARBINARY(32),
        ADD token_verificacao_expira_em DATETIME;
        ALTER TABLE pediagenda.responsavel ADD codigo_ativacao_hash VARBINARY(32),
        ADD codigo_ativacao_expira_em DATETIME, ADD codigo_ativacao_usado_em DATETIME;""")
    sql(migration)
    sql(migration)
    assert before == sql(preservados)
    assert sql("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA='pediagenda' AND (COLUMN_NAME LIKE '%verificacao%' OR COLUMN_NAME LIKE '%ativacao%' OR COLUMN_NAME LIKE 'email_verificado%');") == "0"

    # Conta da API separada da conta que prepara as fixtures.
    sql("CREATE USER IF NOT EXISTS 'pediagenda_api_teste'@'%' IDENTIFIED BY 'apiLocalTeste123'; "
        "GRANT ALL ON pediagenda.* TO 'pediagenda_api_teste'@'%';")
    env = os.environ.copy()
    env["PEDIAGENDA_CONEXAO"] = (
        f"Server={HOST};Port={PORT};Database=pediagenda;User ID=pediagenda_api_teste;"
        "Password=apiLocalTeste123;SslMode=Disabled;")
    env["PEDIAGENDA_JWT_CHAVE"] = TOKEN_KEY
    env["ASPNETCORE_ENVIRONMENT"] = "Production"
    log_path = Path(os.environ.get("PEDIAGENDA_TESTE_LOG", "/tmp/pediagenda-api-testes.log"))
    with log_path.open("w") as log:
        api = subprocess.Popen([os.environ.get("DOTNET_BIN", "dotnet"), "run", "--no-build",
            "--no-launch-profile", "--project", str(ROOT / "api/PediAgenda.Api.csproj"),
            "--urls", BASE], env=env, stdout=log, stderr=subprocess.STDOUT)
        try:
            for _ in range(100):
                try:
                    with HTTP.open(BASE + "/api/saude", timeout=1):
                        break
                except (urllib.error.URLError, TimeoutError):
                    if api.poll() is not None:
                        raise AssertionError(log_path.read_text())
                    time.sleep(.2)
            else:
                raise AssertionError("API não iniciou: " + log_path.read_text())
            scenarios()
        finally:
            api.terminate()
            try:
                api.wait(timeout=10)
            except subprocess.TimeoutExpired:
                api.kill()
    # O relatório pode ser salvo como artifact no CI; não contém tokens nem senhas.
    report = {"verificacoes_http": len(CHECKS), "migracao_repeticoes": 3, "cenarios": CHECKS}
    out = Path(os.environ.get("PEDIAGENDA_TESTE_RESULTADO", "/tmp/pediagenda-resultados.json"))
    out.write_text(json.dumps(report, ensure_ascii=False, indent=2))
    print(f"OK: {len(CHECKS)} verificações HTTP; migração atual/legada/repetida preservou os registros.")
    print("Relatório:", out)


def scenarios():
    today = dt.datetime.now(ZoneInfo("America/Sao_Paulo")).date()
    future = today + dt.timedelta(days=10)
    past = today - dt.timedelta(days=2)
    resp = login("maria@teste.local")
    other = login("ana.oliveira@example.com")
    med = login("medico.teste@example.com")  # id_usuario=4, id_medico=2
    med_other = login("helena.duarte@example.com")  # id_medico=3
    reception = login("recepcao.teste@example.com")

    for i in range(5):
        request("GET", "/api/saude", 200)
        login("maria@teste.local")
        request("GET", "/api/auth/eu", 200, token=resp)
        request("GET", "/api/auth/eu", 401)
        request("POST", "/api/auth/login", 401, {"email": f"inexistente{i}@teste.local", "senha": "senhaDemo123"})
        request("POST", "/api/auth/login", 400, raw=b'{"email":')
        request("POST", "/api/auth/login", 400, {"email": "maria@teste.local", "senha": "curta"})
        request("GET", "/api/nao-existe", 404)
        request("DELETE", "/api/saude", 405)

        email = f"precadastro{i}@teste.local"
        cpf = str(70000000000 + i)
        pre = request("POST", "/api/responsaveis", 201,
            {"nome": f"Responsável {i}", "cpf": cpf, "email": email, "telefone": "11912345678"}, reception)
        assert pre["ativo"] is False
        request("POST", "/api/auth/login", 403, {"email": email, "senha": "senhaDemo123"})
        registration = {"cpf": cpf, "email": email, "senha": "senhaNova123", "aceiteTermos": True}
        request("POST", "/api/cadastro/responsavel", 400, {**registration, "email": "errado@teste.local"})
        request("POST", "/api/cadastro/responsavel", 400, {**registration, "aceiteTermos": False})
        done = request("POST", "/api/cadastro/responsavel", 201, registration)
        assert "token" not in done
        login(email, "senhaNova123")
        request("POST", "/api/cadastro/responsavel", 409, {**registration, "senha": "outraSenha123"})
        assert sql(f"SELECT termos_aceitos_em IS NOT NULL FROM pediagenda.usuario WHERE id_usuario={pre['id']};") == "1"

        patient = request("POST", "/api/pacientes", 201,
            {"nome": f"Paciente API {i}", "dataNascimento": "2021-03-14"}, resp)
        request("POST", "/api/pacientes", 409,
            {"nome": f"Paciente API {i}", "dataNascimento": "2021-03-14"}, resp)
        request("POST", "/api/pacientes", 400,
            {"nome": "", "dataNascimento": str(future)}, resp)
        assert patient["id"] in [p["id"] for p in request("GET", "/api/pacientes", 200, token=resp)]

        day = str(future + dt.timedelta(days=i))
        opened = request("POST", "/api/horarios", 201,
            {"idMedico": 3, "data": day, "inicio": "08:00", "fim": "12:00", "duracao": 30}, med)
        assert opened["criados"] == 8  # médico sempre usa sua própria agenda.
        agenda = request("GET", f"/api/agenda?data={day}", 200, token=med)
        assert all(m["id"] == 2 for m in agenda["medicos"])
        hours = agenda["medicos"][0]["horarios"]
        occupied, free = hours[0]["id"], hours[1]["id"]
        available = request("GET", f"/api/horarios?data={day}&medico=2", 200, token=resp)
        assert occupied in [h["id"] for m in available["medicos"] for h in m["horarios"]]
        request("GET", "/api/horarios?data=invalida", 400, token=resp)
        request("GET", f"/api/horarios?data={day}&medico=-1", 400, token=resp)
        request("GET", "/api/agenda?data=invalida", 400, token=med)
        request("POST", "/api/horarios", 400,
            {"data": day, "inicio": "12:00", "fim": "08:00", "duracao": 3}, med)

        request("PATCH", f"/api/horarios/{free}/bloquear", 200, token=med)
        request("POST", "/api/consultas", 409,
            {"idPaciente": patient["id"], "idHorario": free, "tipoAtendimento": "PARTICULAR"}, resp)
        request("PATCH", f"/api/horarios/{free}/desbloquear", 200, token=med)
        booking = {"idPaciente": patient["id"], "idHorario": occupied, "tipoAtendimento": "PARTICULAR"}
        request("POST", "/api/consultas", 404, booking, other)
        c = request("POST", "/api/consultas", 201, booking, resp)
        request("POST", "/api/consultas", 409, booking, resp)
        request("GET", "/api/consultas", 200, token=resp)
        request("PATCH", f"/api/horarios/{occupied}/bloquear", 409, token=med)
        request("PATCH", f"/api/horarios/{free}/bloquear", 403, token=med_other)
        request("PATCH", f"/api/consultas/{c['id']}/confirmar", 403, token=med_other)
        request("PATCH", f"/api/consultas/{c['id']}/confirmar", 200, token=reception)
        request("PATCH", f"/api/consultas/{c['id']}/confirmar", 409, token=med)
        request("PATCH", f"/api/consultas/{c['id']}/realizar", 409, token=med)
        request("POST", f"/api/consultas/{c['id']}/prontuario", 409, {"observacoes": "Consulta futura"}, med)
        request("PATCH", f"/api/consultas/{c['id']}/cancelar", 404, token=other)
        request("PATCH", f"/api/consultas/{c['id']}/cancelar", 200, token=resp)
        request("PATCH", f"/api/consultas/{c['id']}/cancelar", 409, token=resp)
        request("POST", f"/api/consultas/{c['id']}/prontuario", 409, {"observacoes": "Consulta cancelada"}, med)

        # Consultas passadas, criadas só na instância descartável para testar atendimento.
        hid = int(sql(f"""INSERT INTO pediagenda.horario (data_horario,hora_inicio,hora_fim,id_agenda)
            VALUES ('{past}', '{8+i}:00', '{8+i}:30', 2); SELECT LAST_INSERT_ID();"""))
        cid = int(sql(f"""INSERT INTO pediagenda.consulta(tipo_atendimento,id_paciente,id_horario)
            VALUES('PARTICULAR',{patient['id']},{hid}); SELECT LAST_INSERT_ID();"""))
        request("PATCH", f"/api/consultas/{cid}/realizar", 403, token=reception)
        request("PATCH", f"/api/consultas/{cid}/realizar", 403, token=med_other)
        request("PATCH", f"/api/consultas/{cid}/realizar", 200, token=med)
        request("PATCH", f"/api/consultas/{cid}/realizar", 409, token=med)
        request("POST", f"/api/consultas/{cid}/prontuario", 403, {"observacoes": "Teste"}, resp)
        request("POST", f"/api/consultas/{cid}/prontuario", 404, {"observacoes": "Teste"}, med_other)
        request("POST", f"/api/consultas/{cid}/prontuario", 400, {"observacoes": " "}, med)
        record = request("POST", f"/api/consultas/{cid}/prontuario", 201,
            {"observacoes": f"Atendimento de teste {i}: avaliação e orientações."}, med)
        assert record["idConsulta"] == cid
        request("POST", f"/api/consultas/{cid}/prontuario", 409, {"observacoes": "Não sobrescrever"}, med)
        request("GET", f"/api/consultas/{cid}/prontuario", 200, token=med)
        request("GET", f"/api/consultas/{cid}/prontuario", 404, token=med_other)
        request("GET", f"/api/pacientes/{patient['id']}/prontuarios", 200, token=med)
        request("GET", f"/api/pacientes/{patient['id']}/prontuarios", 404, token=med_other)
        request("GET", "/api/medico/pacientes", 200, token=med)

        report = request("POST", "/api/relatorios", 201,
            {"idMedico": 3, "inicio": str(past), "fim": day}, med)
        assert report["medico"]["id"] == 2 and report["resumo"]["realizadas"] >= i + 1
        rid = report["id"]
        request("GET", "/api/relatorios", 200, token=med)
        request("GET", f"/api/relatorios/{rid}", 200, token=med)
        request("GET", f"/api/relatorios/{rid}", 403, token=med_other)
        csv = request("GET", f"/api/relatorios/{rid}/exportar", 200, token=med)
        assert "Paciente API" in csv
        request("GET", f"/api/relatorios/{rid}/exportar", 403, token=med_other)
        request("POST", "/api/relatorios", 400, {"inicio": day, "fim": str(past)}, med)

    # Proteção de TODAS as rotas autenticadas e dos três perfis.
    routes = [
        ("GET", "/api/auth/eu", None), ("POST", "/api/responsaveis", resp),
        ("GET", "/api/pacientes", med), ("POST", "/api/pacientes", med),
        ("GET", "/api/horarios?data=2030-01-01", None),
        ("GET", "/api/consultas", med), ("POST", "/api/consultas", med),
        ("PATCH", "/api/consultas/1/cancelar", med),
        ("POST", "/api/horarios", resp), ("GET", "/api/agenda?data=2030-01-01", resp),
        ("PATCH", "/api/horarios/1/bloquear", resp), ("PATCH", "/api/horarios/1/desbloquear", resp),
        ("PATCH", "/api/consultas/1/confirmar", resp), ("PATCH", "/api/consultas/1/realizar", reception),
        ("POST", "/api/relatorios", resp), ("GET", "/api/relatorios", resp),
        ("GET", "/api/relatorios/1", resp), ("GET", "/api/relatorios/1/exportar", resp),
        ("GET", "/api/medico/pacientes", reception),
        ("GET", "/api/pacientes/1/prontuarios", resp),
        ("GET", "/api/consultas/1/prontuario", reception),
        ("POST", "/api/consultas/1/prontuario", reception),
    ]
    for method, path, wrong_role in routes:
        for _ in range(5):
            request(method, path, 401, body={} if method == "POST" else None)
            if wrong_role:
                request(method, path, 403, body={} if method == "POST" else None, token=wrong_role)

    now = int(time.time())
    claims = {"sub": "1", "name": "Maria", "email": "maria@teste.local", "role": "RESPONSAVEL",
              "iss": "PediAgenda.Api", "aud": "PediAgenda.App", "nbf": now-7200, "exp": now-3600}
    for payload in (claims, {**claims, "exp": now+3600, "sub": "invalido"},
                    {**claims, "exp": now+3600, "role": "MEDICO"},
                    {**claims, "exp": now+3600, "aud": "outro-app"}):
        request("GET", "/api/auth/eu", 401, token=jwt(payload))
    request("GET", "/api/auth/eu", 401, token=resp + "x")
    sql("UPDATE pediagenda.usuario SET ativo=FALSE WHERE id_usuario=1;")
    request("GET", "/api/auth/eu", 401, token=resp)
    request("POST", "/api/cadastro/responsavel", 409,
        {"cpf": "11122233344", "email": "maria@teste.local", "senha": "tomarConta123", "aceiteTermos": True})
    sql("UPDATE pediagenda.usuario SET ativo=TRUE WHERE id_usuario=1;")
    login("maria@teste.local")

    # Corridas reais: somente uma reserva, um paciente e uma conclusão de cadastro.
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        def competing_patient(i):
            # Não presume qual thread vencerá.
            return parallel_request("POST", "/api/pacientes", {"nome": "Paciente concorrente", "dataNascimento": "2021-01-01"}, resp)
        results = list(pool.map(competing_patient, range(8)))
    assert sorted(results) == [201] + [409]*7, results
    concurrent_booking(resp, med, med_other, future + dt.timedelta(days=20))

    # Duas conclusões de cadastro não podem trocar a senha vencedora.
    request("POST", "/api/responsaveis", 201,
        {"nome": "Cadastro concorrente", "cpf": "71000000001", "email": "corrida@teste.local",
         "telefone": "11912345678"}, reception)
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        results = list(pool.map(lambda i: parallel_request("POST", "/api/cadastro/responsavel",
            {"cpf": "71000000001", "email": "corrida@teste.local", "senha": f"senhaConcorrente{i}",
             "aceiteTermos": True}), range(8)))
    assert sorted(results) == [201] + [409]*7, results
    login("corrida@teste.local", f"senhaConcorrente{results.index(201)}")

    # Cadastro de prontuário conclui o atendimento na mesma transação.
    for i in range(5):
        hid = int(sql(f"""INSERT INTO pediagenda.horario(data_horario,hora_inicio,hora_fim,id_agenda)
            VALUES('{past}','{14+i}:00','{14+i}:30',2); SELECT LAST_INSERT_ID();"""))
        cid = int(sql(f"""INSERT INTO pediagenda.consulta(tipo_atendimento,id_paciente,id_horario)
            VALUES('CONVENIO',1,{hid}); SELECT LAST_INSERT_ID();"""))
        request("POST", f"/api/consultas/{cid}/prontuario", 201, {"observacoes": "Atendimento completo"}, med)
        assert sql(f"SELECT status FROM pediagenda.consulta WHERE id_consulta={cid};") == "REALIZADA"
        request("GET", f"/api/consultas/{cid}/prontuario", 200, token=med)
        request("POST", "/api/consultas/999999/prontuario", 404, {"observacoes": "Sem consulta"}, med)
        request("POST", "/api/consultas/3/prontuario", 400, {"observacoes": "😀"*17000}, med)

    # Um médico desativado não recebe novas reservas por ID de horário.
    day = str(future + dt.timedelta(days=20))
    slots = request("GET", f"/api/horarios?data={day}&medico=3", 200, token=resp)["medicos"]
    # Pode ter sido o vencedor da corrida de reservas; use o último bloco livre.
    slot = slots[0]["horarios"][-1]["id"]
    sql("UPDATE pediagenda.usuario SET ativo=FALSE WHERE id_usuario=10;")
    request("POST", "/api/consultas", 409,
        {"idPaciente": 1, "idHorario": slot, "tipoAtendimento": "PARTICULAR"}, resp)
    sql("UPDATE pediagenda.usuario SET ativo=TRUE WHERE id_usuario=10;")

    # Incrementos simultâneos não podem perder tentativas de senha.
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        results = list(pool.map(lambda _: parallel_request("POST", "/api/auth/login",
            {"email": "marcos.ribeiro@example.com", "senha": "senhaErrada123"}), range(8)))
    assert all(c in (401, 423) for c in results) and 423 in results, results
    request("POST", "/api/auth/login", 423,
        {"email": "marcos.ribeiro@example.com", "senha": "senhaDemo123"})

    # Falha de banco deve ter JSON estável e não devolver detalhes internos.
    sql("REVOKE SELECT ON pediagenda.* FROM 'pediagenda_api_teste'@'%';")
    # Permissões de banco ficam em cache nas conexões MySQL abertas.
    # Encerra somente as conexões da conta descartável para forçar reconexão.
    ids = sql("SELECT ID FROM information_schema.PROCESSLIST WHERE USER='pediagenda_api_teste';")
    if ids:
        sql(" ".join(f"KILL CONNECTION {int(i)};" for i in ids.splitlines()))
    try:
        error = request("GET", "/api/auth/eu", 503, token=resp)
        assert error["codigo"] == "BANCO_INDISPONIVEL"
        request("GET", "/api/saude", 200)
    finally:
        sql("GRANT SELECT ON pediagenda.* TO 'pediagenda_api_teste'@'%';")


def parallel_request(method, path, body, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(BASE + path, data=json.dumps(body).encode(), headers=headers, method=method)
    try:
        r = HTTP.open(req, timeout=30)
    except urllib.error.HTTPError as e:
        r = e
    with r:
        code = r.status
        r.read()
    CHECKS.append({"metodo": method, "rota": path, "status": code, "cenario": "concorrencia"})
    return code


def concurrent_booking(resp, med, other_med, day):
    day = str(day)
    request("POST", "/api/horarios", 201,
        {"data": day, "inicio": "09:00", "fim": "10:00", "duracao": 30}, med)
    request("POST", "/api/horarios", 201,
        {"data": day, "inicio": "09:00", "fim": "10:00", "duracao": 30}, other_med)
    slots = request("GET", f"/api/horarios?data={day}", 200, token=resp)["medicos"]
    ids = [m["horarios"][0]["id"] for m in slots]
    assert len(ids) == 2
    # A mesma criança não pode ocupar duas agendas no mesmo intervalo.
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        results = list(pool.map(lambda i: parallel_request("POST", "/api/consultas",
            {"idPaciente": 1, "idHorario": ids[i % 2], "tipoAtendimento": "PARTICULAR"}, resp), range(8)))
    assert sorted(results) == [201] + [409]*7, results

    # Duas solicitações de abertura idênticas não criam sobreposições.
    opening = {"data": str(dt.date.fromisoformat(day) + dt.timedelta(days=1)),
               "inicio": "09:00", "fim": "10:00", "duracao": 20}
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        results = list(pool.map(lambda _: parallel_request("POST", "/api/horarios", opening, med), range(8)))
    assert all(c == 201 for c in results), results
    agenda = request("GET", f"/api/agenda?data={opening['data']}", 200, token=med)
    assert len(agenda["medicos"][0]["horarios"]) == 3


if __name__ == "__main__":
    main()
