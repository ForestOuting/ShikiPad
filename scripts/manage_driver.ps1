[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Install', 'Uninstall')]
    [string]$Action,
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$installer = Join-Path $root 'driver\install-interception.exe'
$nativeArgument = if ($Action -eq 'Install') { '/install' } else { '/uninstall' }
$verb = if ($Action -eq 'Install') { '安装' } else { '卸载' }
$exitCode = 1
$waitForClose = !$CheckOnly

try {
    if (!(Test-Path -LiteralPath $installer -PathType Leaf)) {
        throw "找不到驱动安装器：$installer。请完整解压 ShikiPad，保留 driver 和 scripts 文件夹。"
    }

    if ($CheckOnly) {
        Write-Output "Installer: $installer"
        Write-Output "Argument: $nativeArgument"
        Write-Output 'Check only: no driver or system settings were changed.'
        exit 0
    }

    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    try {
        $principal = New-Object Security.Principal.WindowsPrincipal($identity)
        $isAdministrator = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    } finally {
        $identity.Dispose()
    }

    if (!$isAdministrator) {
        Write-Host "正在申请管理员权限以$verb Interception，请确认 Windows 弹出的权限提示。"
        # This is an interactive installer console: keep its result visible until Enter.
        $arguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -File "{0}" -Action {1}' -f $PSCommandPath, $Action
        $child = Start-Process -FilePath "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -ArgumentList $arguments -Verb RunAs -WindowStyle Normal -Wait -PassThru
        if ($null -eq $child.ExitCode) { throw '无法取得管理员窗口的执行结果，请查看该窗口中的提示。' }
        $exitCode = $child.ExitCode
        $waitForClose = $false
    } else {
        Write-Host "即将$verb Interception 驱动。"
        Write-Host '请先关闭 ShikiPad；安装或卸载成功后，都需要手动重启 Windows。'
        if (Get-Process -Name ShikiPad -ErrorAction SilentlyContinue) {
            throw 'ShikiPad 正在运行。请先关闭程序，再双击这个入口。'
        }

        & $installer $nativeArgument
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw "驱动${verb}失败，退出代码：$exitCode。请保留上方错误信息。"
        }
        Write-Host "`nInterception ${verb}命令已成功完成。" -ForegroundColor Green
        Write-Host '请保存工作，然后从 Windows 电源菜单选择“重启”。' -ForegroundColor Yellow
        Write-Host '重启前不要启动 ShikiPad 或需要检查驱动的游戏。本工具不会自动重启电脑。'
    }
} catch {
    if ($exitCode -eq 0) { $exitCode = 1 }
    Write-Host "`n操作未完成：$($_.Exception.Message)" -ForegroundColor Red
} finally {
    if ($waitForClose) {
        [void](Read-Host '按 Enter 关闭窗口')
    }
}
exit $exitCode
