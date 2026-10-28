#Requires -Version 5.1
param(
    [string]$SonarUrl     = "http://localhost:9000",
    [string]$ProjectKey   = "techchallenger-fase1",
    [string]$NewAdminPass = "Admin@sonar1"
)

$ErrorActionPreference = "Stop"
$env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"

function Invoke-SonarApi {
    param([string]$Path, [string]$Method = "GET", [string]$Body = "", [string]$User = "admin", [string]$Pass = $NewAdminPass)
    $enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${User}:${Pass}"))
    $p = @{ Uri = "$SonarUrl$Path"; Method = $Method; Headers = @{ Authorization = "Basic $enc" } }
    if ($Body) { $p.Body = $Body; $p.ContentType = "application/x-www-form-urlencoded" }
    return Invoke-RestMethod @p
}

function Ensure-DotnetTool {
    param([string]$Name, [string]$Package = $Name)
    $list = & dotnet tool list -g 2>&1
    if ($list -notmatch [Regex]::Escape($Name)) {
        Write-Host "Instalando $Name..." -ForegroundColor Yellow
        dotnet tool install --global $Package
    } else {
        Write-Host "$Name ja instalado." -ForegroundColor DarkGray
    }
}

# [1/9] Aguardar SonarQube
Write-Host "`n[1/9] Aguardando SonarQube inicializar..." -ForegroundColor Cyan
$max = 300
$elapsed = 0
while ($true) {
    $sonarStatus = $null
    try { $sonarStatus = Invoke-RestMethod "$SonarUrl/api/system/status" -ErrorAction SilentlyContinue } catch {}
    if ($sonarStatus -and $sonarStatus.status -eq "UP") {
        Write-Host "     SonarQube online." -ForegroundColor Green
        break
    }
    if ($elapsed -ge $max) { throw "Timeout: SonarQube nao iniciou em $max s." }
    Start-Sleep 5
    $elapsed += 5
    Write-Host "     ... $elapsed/$max s"
}

# [2/9] Alterar senha do admin
Write-Host "`n[2/9] Configurando senha do admin..." -ForegroundColor Cyan
$enc = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
$changeBody = "login=admin&previousPassword=admin&password=$([Uri]::EscapeDataString($NewAdminPass))"
try {
    Invoke-RestMethod "$SonarUrl/api/users/change_password" -Method Post -Headers @{ Authorization = "Basic $enc" } -Body $changeBody -ContentType "application/x-www-form-urlencoded" | Out-Null
    Write-Host "     Senha alterada para '$NewAdminPass'." -ForegroundColor Green
} catch {
    Write-Host "     Senha ja foi alterada anteriormente - continuando." -ForegroundColor Yellow
}

# [3/9] Criar projeto
Write-Host "`n[3/9] Criando projeto '$ProjectKey'..." -ForegroundColor Cyan
try {
    Invoke-SonarApi "/api/projects/create" "POST" "name=TechChallenger+Fase+1&project=$ProjectKey" | Out-Null
    Write-Host "     Projeto criado." -ForegroundColor Green
} catch {
    Write-Host "     Projeto ja existe - continuando." -ForegroundColor Yellow
}

# [4/9] Gerar token de analise
Write-Host "`n[4/9] Gerando token de analise..." -ForegroundColor Cyan
$tokenName = "ci-$(Get-Date -Format 'yyyyMMddHHmmss')"
$tok = (Invoke-SonarApi "/api/user_tokens/generate" "POST" "name=$tokenName").token
Write-Host "     Token '$tokenName' gerado." -ForegroundColor Green

# [5/9] Instalar ferramentas globais
Write-Host "`n[5/9] Verificando ferramentas .NET..." -ForegroundColor Cyan
Ensure-DotnetTool "dotnet-sonarscanner"

# [6/9] Limpar artefatos anteriores
Write-Host "`n[6/9] Limpando artefatos anteriores..." -ForegroundColor Cyan
Remove-Item -Recurse -Force "coverage"   -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ".sonarqube" -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path "coverage/raw" | Out-Null

# [7/9] Iniciar SonarScanner
Write-Host "`n[7/9] Iniciando SonarScanner (begin)..." -ForegroundColor Cyan
$beginArgs = @(
    "begin",
    "/k:$ProjectKey",
    "/d:sonar.host.url=$SonarUrl",
    "/d:sonar.token=$tok",
    "/d:sonar.cs.opencover.reportsPaths=coverage/raw/**/coverage.opencover.xml",
    "/d:sonar.exclusions=**/Migrations/**,**/obj/**,**/bin/**",
    "/d:sonar.test.inclusions=**/*.Tests/**"
)
& dotnet sonarscanner @beginArgs

# [8/9] Build e testes
Write-Host "`n[8/9] Build e testes com cobertura..." -ForegroundColor Cyan

dotnet build src/TechChallenger-fase1.sln -c Release --no-incremental

dotnet test src/TechChallenger-fase1.sln --filter "FullyQualifiedName~Tests" --collect:"XPlat Code Coverage" --results-directory "coverage/raw" --no-build -c Release -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# [9/9] Finalizar analise
Write-Host "`n[9/9] Enviando resultados para o SonarQube (end)..." -ForegroundColor Cyan
& dotnet sonarscanner end "/d:sonar.token=$tok"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host " Analise concluida!" -ForegroundColor Green
Write-Host " Dashboard : $SonarUrl/dashboard?id=$ProjectKey" -ForegroundColor Cyan
Write-Host " Login     : admin" -ForegroundColor Cyan
Write-Host " Senha     : $NewAdminPass" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Green
