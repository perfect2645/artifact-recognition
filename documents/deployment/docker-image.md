# onborad docker desktop

## Step 1 : open powershell and enter the project path.

```
Set-Location 'D:\xxx\your roject root path'
```

- open powershell and

## Step 2 : publish target project

```
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

```
docker build -f .\Dockerfile.publish -t artifact-api:local .
```

| Part / 部分 | Meaning / 含义 |
| --- | --- |
| `docker build` | Build an image / 构建镜像 |
| `-f .\Dockerfile.publish` | Select the Dockerfile / 指定 Dockerfile |
| `-t artifact-api:local` | Assign the image name and tag / 设置镜像名称和标签 |
| `.` | Use the current directory as the build context / 使用当前目录作为构建上下文 |

**We don’t need to change the Dockerfile’s Production default:** we’ll override it with `Development` when starting the container. We’ll also mount the certificate and configure HTTPS at that stage.
**暂时不需要修改 Dockerfile 中的 Production 默认值：**启动容器时会覆盖为 `Development`，并在那时挂载证书、配置 HTTPS。

Building the image will **not update or stop the existing container**.
构建镜像**不会更新或停止现有容器**。

## Step 4 : Remove the old container

·············

**First, note that removing the container deletes its internal log files.** If you want to keep them, back them up before proceeding:
**注意，删除容器会删除其内部的日志文件。** 如果需要保留，先执行备份：

```
docker cp artifact-api:/app/logs .\container-logs-backup
```

Then stop the old container:
然后停止旧容器：

```
docker stop artifact-api
```

Remove it:
再删除它：

```
docker rm artifact-api
```

These commands **do not delete your image, Windows project files, or certificate**.
这些命令**不会删除镜像、Windows 上的项目文件或证书**。

**Stop here after removing it.** Next, we’ll construct the new `docker run` command together, including Development settings, certificate mounts, and HTTPS port mapping.
**删除完成后先到这里。** 下一步我们一起准备新的 `docker run` 命令，加入 Development 配置、证书挂载和 HTTPS 端口映射。

·············

Let’s create the new container with **Development + HTTPS**. For this step, we’ll expose **HTTPS only** at `[https://localhost:5001](https://localhost:5001)`.
我们来创建启用 **Development + HTTPS** 的新容器。这一步先**只开放 HTTPS**，访问地址为 `[https://localhost:5001](https://localhost:5001)`。

## Step 5: Start the HTTPS container / 第四步：启动 HTTPS 容器

### 1. Set the project path / 设置项目路径

Run this in PowerShell:
在 PowerShell 中执行：

```
$projectPath = "D:\dotNet\personal\projects\artifact-recognition\artifact-service-domain"
```

Confirm that both files exist:
确认下面两个文件存在：

```
Test-Path "$projectPath\appsettings.Development.json"
Test-Path "$projectPath\Configurations\certs\localhost-doraemon.pfx"
```

**Both should return `True`. If either returns `False`, stop here.**
**两条命令都应该返回 `True`。如果有一条返回 `False`，先不要继续。**

### 2. Create and start the container / 创建并启动容器

```
docker run -d `
    --name artifact-api `
    -p 127.0.0.1:5001:8081 `
    -e "ASPNETCORE_ENVIRONMENT=Development" `
    -e "ASPNETCORE_HTTP_PORTS=" `
    -e "Kestrel__Endpoints__Https__Url=[https://0.0.0.0:8081](https://0.0.0.0:8081)" `
    -e "Kestrel__Endpoints__Https__Certificate__Path=/https/localhost-doraemon.pfx" `
    --mount "type=bind,source=$projectPath\appsettings.Development.json,target=/app/appsettings.Development.json,readonly" `
    --mount "type=bind,source=$projectPath\Configurations\certs\localhost-doraemon.pfx,target=/https/localhost-doraemon.pfx,readonly" `
    artifact-api:local
```

**In PowerShell, the backtick must be the last character on each continued line—no trailing spaces.**
**PowerShell 中，反引号必须是续行的最后一个字符，后面不要加空格。**

### What the new options mean / 新参数的含义

| Option / 参数 | Purpose / 作用 |
| --- | --- |
| `-p 127.0.0.1:5001:8081` | Forward Windows port 5001 to container port 8081, for local access only.   将 Windows 的 5001 转发到容器的 8081，仅允许本机访问。 |
| `ASPNETCORE_ENVIRONMENT=Development` | Override the image’s Production default.   覆盖镜像中的 Production 默认环境。 |
| `ASPNETCORE_HTTP_PORTS=` | Clear the default HTTP-port setting for this HTTPS-only test.   清空默认 HTTP 端口设置，本次仅测试 HTTPS。 |
| `Kestrel__Endpoints__Https__Url=…` | Listen on all container IPv4 interfaces at HTTPS port 8081.   在容器所有 IPv4 网络接口的 8081 端口监听 HTTPS。 |
| `Certificate__Path=…` | Use the certificate’s **container path**, not its Windows path.   指定证书的**容器内路径**，而不是 Windows 路径。 |
| `--mount … readonly` | Make a Windows file available inside the container without copying it into the image or allowing the container to modify it.   让容器读取 Windows 文件，不将其打进镜像，也不允许容器修改它。 |

ASP.NET Core treats `__` in environment-variable names as a configuration hierarchy separator. For example:
ASP.NET Core 将环境变量名中的 `__` 解释为配置层级分隔符，例如：

```
Kestrel__Endpoints__Https__Certificate__Path
    ↓
Kestrel:Endpoints:Https:Certificate:Path
```

The certificate password will come from your mounted Development configuration; we won’t put it in the command.
证书密码会从挂载的 Development 配置中读取，不需要写进命令。

## 3. Check startup / 检查启动状态

```
docker ps -a --filter name=artifact-api
docker logs --tail 50 artifact-api
```

**A container ID alone does not prove that startup succeeded.** Share the status and logs so we can confirm that Kestrel loaded the certificate and started listening.
**仅返回容器 ID 不代表启动成功。** 把状态和日志贴过来，我们再确认 Kestrel 是否成功加载证书并开始监听。

Certificate trust on Windows is a separate step—we’ll verify that after the container starts.
Windows 是否信任该自签名证书是另一个问题，我们在容器启动成功后再验证。