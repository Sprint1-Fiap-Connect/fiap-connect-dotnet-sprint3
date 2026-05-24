"""
Reverse proxy que repassa requests pra https://oracleapex.com/ords/alexisrondo/fiapconnect/
usando curl_cffi pra impersonar o TLS fingerprint do Chrome 124.

A app .NET aponta Oracle__BaseUrl pra este proxy (ex: http://oracle-proxy:9000/).
Quando a app faz GET /usuario/RM560384, este proxy faz a request real pro APEX
com handshake TLS identico ao Chrome, burlando o WAF Akamai.
"""

import os
import logging
from urllib.parse import urljoin

from curl_cffi import requests as curl_requests
from fastapi import FastAPI, Request, Response

UPSTREAM = os.environ.get(
    "UPSTREAM_BASE_URL",
    "https://oracleapex.com/ords/alexisrondo/fiapconnect/"
)
IMPERSONATE = os.environ.get("IMPERSONATE_PROFILE", "chrome124")

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger("oracle-proxy")

app = FastAPI()


@app.get("/_proxy/health")
def health():
    return {"status": "ok", "upstream": UPSTREAM, "impersonate": IMPERSONATE}


@app.api_route(
    "/{path:path}",
    methods=["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"]
)
async def proxy(path: str, request: Request):
    target_url = urljoin(UPSTREAM, path)
    if request.url.query:
        target_url = f"{target_url}?{request.url.query}"

    # Repassa headers do client, exceto os que curl_cffi precisa controlar
    # (host, content-length sao recalculados; user-agent vem do profile chrome124)
    skip = {"host", "content-length", "user-agent", "connection", "accept-encoding"}
    fwd_headers = {k: v for k, v in request.headers.items() if k.lower() not in skip}

    body = await request.body()

    log.info(f"-> {request.method} {target_url}")

    try:
        upstream = curl_requests.request(
            method=request.method,
            url=target_url,
            headers=fwd_headers,
            data=body if body else None,
            impersonate=IMPERSONATE,
            timeout=30,
            allow_redirects=False,
        )
    except Exception as ex:
        log.error(f"!! erro upstream: {ex}")
        return Response(
            content=f'{{"erro_proxy":"falha ao contatar upstream","detalhe":"{ex}"}}',
            status_code=502,
            media_type="application/json"
        )

    log.info(f"<- {upstream.status_code} ({len(upstream.content)} bytes)")

    # Filtra headers de resposta que ASGI/uvicorn nao deve passar adiante
    drop = {"content-encoding", "transfer-encoding", "content-length", "connection"}
    resp_headers = {
        k: v for k, v in upstream.headers.items() if k.lower() not in drop
    }

    return Response(
        content=upstream.content,
        status_code=upstream.status_code,
        headers=resp_headers,
    )
