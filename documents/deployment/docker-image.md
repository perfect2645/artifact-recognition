# Docker Desktop deployment guide / Docker Desktop 部署指南

Run all commands below in PowerShell from the Web API project directory, not from this document's directory or the Dockerfile directory.
以下命令均在 PowerShell 中从 Web API 项目目录执行，而不是从本文档目录或 Dockerfile 目录执行。

The deployment files are [Dockerfile.publish](../../artifact-service-domain/devops/docker/Dockerfile.publish) and its dedicated [Dockerfile.publish.dockerignore](../../artifact-service-domain/devops/docker/Dockerfile.publish.dockerignore).
部署文件为 [Dockerfile.publish](../../artifact-service-domain/devops/docker/Dockerfile.publish) 及其专用的 [Dockerfile.publish.dockerignore](../../artifact-service-domain/devops/docker/Dockerfile.publish.dockerignore)。

This guide uses manual `docker run` deployment. For Compose-managed containers, follow the [Compose guide](./docker-compose.md) instead; do not run both workflows for the same container.
本指南使用手动 `docker run` 部署。由 Compose 管理的容器请使用 [Compose 指南](./docker-compose.md)，不要对同一个容器混用两套流程。

```text
artifact-recognition\
├── artifact-service-domain\
│   ├── devops\docker\
│   │   ├── Dockerfile.publish
│   │   └── Dockerfile.publish.dockerignore
│   ├── Configurations\certs\
│   ├── publish\
│   ├── logs\
│   └── appsettings.Development.json
└── documents\deployment\
    └── docker-image.md

```

## Step 1 : open powershell and enter the project path.

```powershell
Set-Location 'D:\projects\artifact-recognition\artifact-service-domain'

# Office PC alternative / 办公室电脑路径：
# Set-Location 'D:\dotNet\personal\projects\artifact-recognition\artifact-service-domain'

```

Use the path that exists on your PC.
请使用当前电脑上实际存在的项目路径。

## Step 2 : publish target project

```powershell
dotnet publish .\artifact.service.domain.csproj -c Release -o .\publish --self-contained false -p:UseAppHost=false

```

--self-contained false
Creates a framework-dependent deployment: the published application does not include the .NET runtime.

生成依赖框架的部署产物：发布文件中不包含 .NET 运行时。

-p:UseAppHost=false
-p: passes an MSBuild property. UseAppHost=false disables generation of the platform-specific native launcher—for example, a Windows .exe.

-p: 用于传入 MSBuild 属性。UseAppHost=false 禁止生成平台专用的原生启动程序，例如 Windows 的 .exe。

## Step 3: Build docker image

Run this from your Web API project directory:
在 Web API 项目目录中执行：

Publish successfully after any application code change before building. This Dockerfile packages the existing published files; it does not compile source code.
应用代码修改后，须先发布成功再构建。此 Dockerfile 打包现有发布文件，不会编译源代码。

```powershell
# Choose a unique tag for this build / 为本次构建选择唯一标签
$env:IMAGE_TAG = "dev-20261030-1.0.0"
docker build -f .\devops\docker\Dockerfile.publish -t "artifact-api:${env:IMAGE_TAG}" .

```

The tag above is an example. Choose a new tag for each new build, then reuse that exact tag for deployment. A tag identifies a build by convention; it does not automatically verify its source version.
上面的标签仅为示例。每次新构建选择新标签，部署时使用完全相同的标签。标签通过约定区分构建，不会自动验证源码版本。

| Part / 部分                             | Meaning / 含义                                                              |
| --------------------------------------- | --------------------------------------------------------------------------- |
| `docker build`                          | Build an image / 构建镜像                                                   |
| `-f .\devops\docker\Dockerfile.publish` | Select the relocated Dockerfile / 指定移动后的 Dockerfile 路径              |
| `-t "artifact-api:${env:IMAGE_TAG}"`    | Assign the image name and selected version tag / 设置镜像名称和所选版本标签 |
| `.`                                     | Use the current directory as the build context / 使用当前目录作为构建上下文 |

The build context is still the Web API project directory. Therefore, `COPY publish/ ./` in the Dockerfile reads the project's `publish` folder, not a folder relative to `devops\docker`. Docker uses the dedicated ignore file next to this Dockerfile instead of the project's root `.dockerignore`.
构建上下文仍是 Web API 项目目录。因此，Dockerfile 中的 `COPY publish/ ./` 读取项目下的 `publish` 文件夹，而不是相对于 `devops\docker` 查找。Docker 使用该 Dockerfile 旁边的专用忽略文件，而不是项目根目录的 `.dockerignore`。

**We don’t need to change the Dockerfile’s Production default:** we’ll override it with `Development` when starting the container. We’ll also mount the certificate and configure HTTPS at that stage.
**暂时不需要修改 Dockerfile 中的 Production 默认值：**启动容器时会覆盖为 `Development`，并在那时挂载证书、配置 HTTPS。

Building the image will **not update or stop the existing container**.
构建镜像**不会更新或停止现有容器**。

Keep the previous image until the new deployment passes verification so that rollback remains possible.
新部署验证通过前保留旧镜像，以便回滚。

## Step 4 : Remove the old container

Skip this step if no `artifact-api` container exists.
如果不存在 `artifact-api` 容器，请跳过此步骤。

Stop the old container first:
先停止旧容器：

```powershell
docker stop artifact-api

```

**Removing a container deletes logs stored only in its writable layer.** If you need those logs, back them up before removal. Bind-mounted Windows logs remain in their host folder.
**删除容器会删除仅存于其可写层中的日志。** 如果需要这些日志，请在删除前备份。绑定挂载的 Windows 日志仍保留在宿主机文件夹中。

```powershell
docker cp artifact-api:/app/logs .\container-logs-backup

```

Remove it:
再删除它：

```powershell
docker rm artifact-api

```

These commands **do not delete your image, Windows project files, or certificate**.
这些命令**不会删除镜像、Windows 上的项目文件或证书**。

Let’s create the new container with **Development + HTTPS**. For this step, we’ll expose **HTTPS only** at [[https://localhost:5001](https://localhost:5001)](https://localhost:5001).
我们来创建启用 **Development + HTTPS** 的新容器。这一步先**只开放 HTTPS**，访问地址为 [[https://localhost:5001](https://localhost:5001)](https://localhost:5001)。

## Step 5: Start the HTTPS container / 第五步：启动 HTTPS 容器

### 1. Set the paths / 设置路径

Run this in the same PowerShell terminal, with the project directory from Step 1 as the current directory:
在同一个 PowerShell 终端中执行，当前目录须为第一步设置的项目目录：

```powershell
$projectPath = (Get-Location).Path
$certificateName = "localhost-artifact.pfx"
$certificatePath = Join-Path $projectPath "Configurations\certs\$certificateName"
$logPath = Join-Path $projectPath "logs"

New-Item -ItemType Directory -Path $logPath -Force

```

The default certificate is the home PC certificate, `localhost-artifact.pfx` (office PC: `localhost-doraemon.pfx`). On the office PC, change only `$certificateName` above before running the remaining lines. The certificate password in the mounted Development configuration must match the selected certificate; do not put the password in this guide.
默认证书为家里电脑的 `localhost-artifact.pfx`（办公室电脑：`localhost-doraemon.pfx`）。在办公室电脑上，只需先修改上面的 `$certificateName`，再执行其余语句。挂载的 Development 配置中的证书密码必须与所选证书匹配；不要将密码写入本指南。

If you already use another Windows log folder, set `$logPath` to that same folder to preserve access to the existing logs.
如果之前已使用其他 Windows 日志文件夹，请将 `$logPath` 设置为原来的文件夹，以继续访问已有日志。

Confirm that both files and the log directory exist:
确认两个文件和日志目录均存在：

```powershell
Test-Path -LiteralPath "$projectPath\appsettings.Development.json" -PathType Leaf
Test-Path -LiteralPath $certificatePath -PathType Leaf
Test-Path -LiteralPath $logPath -PathType Container

```

**All three should return `True`. If any returns `False`, stop and correct the path before continuing.**
**三条命令都应该返回 `True`。如果有任何一条返回 `False`，先修正路径再继续。**

PowerShell variables are local to the current session. If you open a new terminal, repeat Step 1 and this path setup, and restore `$env:IMAGE_TAG` to the exact tag built in Step 3. Do not generate a different tag just for deployment.
PowerShell 变量仅存在于当前会话。如果打开新终端，请重新执行第一步和这里的路径设置，并将 `$env:IMAGE_TAG` 恢复为第三步构建的准确标签。不要仅为了部署而生成另一个标签。

### 2. Create and start the container / 创建并启动容器

Check the selected tag before starting; if it is empty, set it to the tag built in Step 3.
启动前检查所选标签；如果为空，请设置为第三步构建的标签。

```powershell
$env:IMAGE_TAG
```

```powershell
docker run -d `
    --name artifact-api `
    -p 127.0.0.1:5001:8081 `
    -e "ASPNETCORE_ENVIRONMENT=Development" `
    -e "ASPNETCORE_HTTP_PORTS=" `
    -e "Kestrel__Endpoints__Https__Url=[https://0.0.0.0:8081](https://0.0.0.0:8081)" `
    -e "Kestrel__Endpoints__Https__Certificate__Path=/https/${certificateName}" `
    --mount "type=bind,source=${projectPath}\appsettings.Development.json,target=/app/appsettings.Development.json,readonly" `
    --mount "type=bind,source=${certificatePath},target=/https/${certificateName},readonly" `
    --mount "type=bind,source=${logPath},target=/app/logs" `
    "artifact-api:${env:IMAGE_TAG}"

```

**In PowerShell, the backtick must be the last character on each continued line—no trailing spaces.**
**PowerShell 中，反引号必须是续行的最后一个字符，后面不要加空格。**

### What the new options mean / 新参数的含义

| Option / 参数                        | Purpose / 作用                                                                                                                                                                             |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `-p 127.0.0.1:5001:8081`             | Forward Windows port 5001 to container port 8081, for local access only. 将 Windows 的 5001 转发到容器的 8081，仅允许本机访问。                                                            |
| `ASPNETCORE_ENVIRONMENT=Development` | Override the image’s Production default. 覆盖镜像中的 Production 默认环境。                                                                                                                |
| `ASPNETCORE_HTTP_PORTS=`             | Clear the default HTTP-port setting for this HTTPS-only test. 清空默认 HTTP 端口设置，本次仅测试 HTTPS。                                                                                   |
| `Kestrel__Endpoints__Https__Url=…`   | Listen on all container IPv4 interfaces at HTTPS port 8081. 在容器所有 IPv4 网络接口的 8081 端口监听 HTTPS。                                                                               |
| `Certificate__Path=…`                | Use the certificate’s **container path**, not its Windows path. 指定证书的**容器内路径**，而不是 Windows 路径。                                                                            |
| `--mount … readonly`                 | Make a Windows file available inside the container without copying it into the image or allowing the container to modify it. 让容器读取 Windows 文件，不将其打进镜像，也不允许容器修改它。 |
| `--mount … target=/app/logs`         | Persist application logs in the Windows log folder; this mount is writable. 将应用日志持久化到 Windows 日志文件夹；此挂载允许写入。                                                        |

The explicit Kestrel endpoint above selects HTTPS port 8081. The Dockerfile's default HTTP port and `EXPOSE 7092` do not prevent this runtime override; `EXPOSE` does not configure the server or publish a port.
上面的显式 Kestrel 端点配置选择 HTTPS 8081。Dockerfile 中的默认 HTTP 端口和 `EXPOSE 7092` 不会阻止运行时覆盖；`EXPOSE` 不负责配置服务器或发布端口。

[ASP.NET](https://ASP.NET) Core treats `__` in environment-variable names as a configuration hierarchy separator. For example:
[ASP.NET](https://ASP.NET) Core 将环境变量名中的 `__` 解释为配置层级分隔符，例如：

```plaintext
Kestrel__Endpoints__Https__Certificate__Path
    ↓
Kestrel:Endpoints:Https:Certificate:Path

```

The certificate password will come from your mounted Development configuration; we won’t put it in the command.
证书密码会从挂载的 Development 配置中读取，不需要写进命令。

### 3. Check startup / 检查启动状态

```powershell
docker ps -a --filter name=artifact-api
docker logs --tail 50 artifact-api
Get-ChildItem -LiteralPath $logPath

```

**A container ID alone does not prove that startup succeeded.** Share the status and logs so we can confirm that Kestrel loaded the certificate and started listening.
**仅返回容器 ID 不代表启动成功。** 把状态和日志贴过来，我们再确认 Kestrel 是否成功加载证书并开始监听。

Certificate trust on Windows is a separate step—we’ll verify that after the container starts.
Windows 是否信任该自签名证书是另一个问题，我们在容器启动成功后再验证。
