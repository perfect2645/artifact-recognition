FROM python:3.14-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

COPY requirements.txt .

RUN python -m pip install --no-cache-dir \
    torch==2.10.0 torchvision==0.25.0 \
    --index-url https://download.pytorch.org/whl/cpu

RUN python -m pip install --no-cache-dir -r requirements.txt

RUN useradd --create-home --uid 10001 worker

COPY --chown=worker:worker src/ ./src/

USER worker

ENTRYPOINT [ "python", "src/controller/artifact_worker.py" ]