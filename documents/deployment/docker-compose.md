Docker Compose quick guide / Docker Compose 操作速查
Use the existing Compose configuration. Run all commands in the same PowerShell terminal.
使用现有的 Compose 配置，所有命令在同一个 PowerShell 终端执行。

1. Prepare / 准备

```powershell
Set-Location 'D:\projects\artifact-recognition\artifact-service-domain'

# Office PC / 办公室电脑：
# Set-Location 'D:\dotNet\personal\projects\artifact-recognition\artifact-service-domain'

$env:CERTIFICATE_NAME = "localhost-artifact.pfx"
# Office certificate / 办公室证书：
# $env:CERTIFICATE_NAME = "localhost-doraemon.pfx"

docker compose version
New-Item -ItemType Directory -Path .\container-logs -Force

Test-Path -LiteralPath .\appsettings.Development.json -PathType Leaf
Test-Path -LiteralPath ".\Configurations\certs\$env:CERTIFICATE_NAME" -PathType Leaf

```

Both checks must return True. The certificate password in the Development configuration must match the selected certificate. The client must trust the certificate, and it must be valid for localhost.
两项检查必须返回 True。Development 配置中的证书密码必须与所选证书匹配；客户端必须信任证书，且证书须对 localhost 有效。2. Publish and build / 发布与构建
Skip if the required image is already built. Continue only after each command succeeds.
如果已构建所需镜像，可以跳过。每条命令成功后再继续。

```powershell
dotnet publish .\artifact.service.domain.csproj -c Release -o .\publish --self-contained false -p:UseAppHost=false

docker build -f .\devops\docker\Dockerfile.publish -t artifact-api:dev .

```

3. Validate Compose / 验证配置

```powershell
docker compose -f .\devops\docker\docker-compose.yaml config --quiet
$LASTEXITCODE

```

Expected: 0. This validates configuration, not runtime access to files or ports.
预期为 0。这里只验证配置，不验证运行时文件访问或端口可用性。4. Remove a manual container, if present / 如有手动创建的旧容器，先移除
Only for the first migration from docker run. Skip if absent or already managed by this Compose project.
仅用于首次从 docker run 迁移；容器不存在或已由本 Compose 项目管理时跳过。

```powershell
docker ps -a --filter "name=^/artifact-api$"

```

Before removing it, confirm its logs are bind-mounted or back them up. Removing a container deletes its writable layer, but not bind-mounted Windows files.
删除前确认日志已绑定挂载或完成备份。删除容器会删除其可写层，但不会删除绑定挂载的 Windows 文件。

```powershell
docker stop artifact-api
docker rm artifact-api

```

5. Start and check / 启动与检查

```powershell
docker compose -f .\devops\docker\docker-compose.yaml up -d

docker compose -f .\devops\docker\docker-compose.yaml ps -a

docker compose -f .\devops\docker\docker-compose.yaml logs --tail 30 artifact-api

```

Expected: Up, HTTPS listening on 8081, environment Development.
预期：状态为 Up，HTTPS 监听 8081，环境为 Development。6. Verify HTTPS and persistent logs / 验证 HTTPS 和持久化日志

```powershell
curl.exe -i https://localhost:5001/api/recognition

Get-ChildItem -LiteralPath .\container-logs -Filter "app-*.log" -File |
    Sort-Object Name -Descending |
    Select-Object -First 1 |
    Get-Content -Tail 10

```

Expected: HTTP 405 with Allow: POST because this request uses GET. Open Swagger at https://localhost:5001/swagger to test POST; an empty artifacts list avoids sending recognition messages.
预期：HTTP 405 且包含 Allow: POST，因为这里发送的是 GET。打开 https://localhost:5001/swagger 测试 POST；使用空 artifacts 列表可避免发送识别消息。
Common commands / 常用命令

```powershell
# Follow logs; Ctrl+C exits following, not the container / 跟踪日志，Ctrl+C 不会停止容器
docker compose -f .\devops\docker\docker-compose.yaml logs -f --tail 20 artifact-api

# Stop without deleting / 停止但不删除
docker compose -f .\devops\docker\docker-compose.yaml stop

# Start again or apply configuration/image changes / 再次启动或应用配置、镜像变更
docker compose -f .\devops\docker\docker-compose.yaml up -d

# Remove Compose containers and default network; Windows logs remain / 删除容器和默认网络，保留 Windows 日志
docker compose -f .\devops\docker\docker-compose.yaml down

```

After code changes: repeat publish → build → up -d. This Compose file does not build the image automatically. In a new terminal, set the project directory and certificate environment variable again.
修改代码后重复 发布 → 构建 → up -d。此 Compose 文件不会自动构建镜像。打开新终端后，需要重新设置项目目录和证书环境变量。
