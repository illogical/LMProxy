# LMProxy

A simple proxy server for LLMs (Large Language Models) that allows you to easily switch between different models and providers.

## Features
- Simple Ollama prompting
- Message queuing with RabbitMQ
- .NET 9 Web API with Aspire support
- Dockerized for easy deployment

## Docker Compose Usage

This project includes a `docker-compose.yml` for running the API and its dependencies (such as RabbitMQ) in containers.

1. Create a `.env` file in the root directory with the following content:

```env
TS_AUTHKEY=<your_auth_key>

```

2. Update AppSettings.json

### Start the services
```sh
docker compose up -d
```
This will build and start all containers in the background.

### Stop the services
```sh
docker compose down
```
This will stop and remove all containers, networks, and volumes created by `up`.

## Roadmap
- Support for queueing prompt requests
- Allow multiple PCs to respond to queued requests (distributed workers)

---
For now, the API supports basic Ollama chat and message queueing. More advanced distributed features are planned for future releases.