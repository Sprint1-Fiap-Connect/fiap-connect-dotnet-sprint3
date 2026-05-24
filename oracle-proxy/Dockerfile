FROM python:3.11-slim

WORKDIR /app

# curl_cffi traz binarios pre-compilados (libcurl-impersonate) nas wheels Linux x64
RUN pip install --no-cache-dir --upgrade pip
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY proxy.py .

EXPOSE 9000

CMD ["uvicorn", "proxy:app", "--host", "0.0.0.0", "--port", "9000"]
