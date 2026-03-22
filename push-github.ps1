# Executa na pasta do projeto (duplo clique ou: powershell -ExecutionPolicy Bypass -File push-github.ps1)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "Estado antes:" -ForegroundColor Cyan
git status

git add -A
git status

$msg = "ci: CI só em PR (sem duplicar deploy); README Actions atualizado"
git commit -m $msg
if ($LASTEXITCODE -ne 0) {
    Write-Host "Commit ignorado (nada a gravar ou já atualizado)." -ForegroundColor Yellow
} else {
    Write-Host "Commit OK." -ForegroundColor Green
}

Write-Host "Push para origin/main..." -ForegroundColor Cyan
git push origin main

Write-Host "Feito. Verifica GitHub Actions." -ForegroundColor Green
pause
