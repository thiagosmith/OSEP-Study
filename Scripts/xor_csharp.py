import sys
import re
import random

# Cores para o terminal
GREEN = "\033[92m"
CYAN = "\033[96m"
YELLOW = "\033[93m"
BOLD = "\033[1m"
END = "\033[0m"

def generate_csharp_xored():
    # Lê a entrada do STDIN
    input_data = sys.stdin.read()

    # O msfvenom -f csharp gera algo como: 0xfc,0x48,0x83...
    # Este regex captura apenas o que está depois do 'x'
    byte_matches = re.findall(r'0x([0-9a-fA-F]{2})', input_data)

    if not byte_matches:
        print(f"{YELLOW}[!] Erro: Nenhum byte no formato 0x00 encontrado na entrada.{END}")
        return

    # Converte de hexadecimal (base 16) para inteiro com segurança
    bytes_list = [int(b, 16) for b in byte_matches]

    # Gera chave aleatória (1 a 255)
    key = random.randint(1, 255)

    # Aplica o XOR
    xored_bytes = [b ^ key for b in bytes_list]

    # Formatação das linhas para o C#
    csharp_array_lines = []
    for i in range(0, len(xored_bytes), 12):
        chunk = xored_bytes[i:i+12]
        line = ", ".join([f"0x{b:02x}" for b in chunk])
        csharp_array_lines.append(f"            {line}")

    formatted_array = ",\n".join(csharp_array_lines)

    # Output final
    print(f"\n{GREEN}{BOLD}[+] Shellcode processado com sucesso!{END}")
    print(f"{CYAN}{BOLD}// --- C# XOR OUTPUT ---{END}")
    print(f"{YELLOW}// Chave XOR: 0x{key:02x} ({key}){END}\n")

    print(f"byte[] buf = new byte[{len(xored_bytes)}] {{")
    print(formatted_array)
    print("};\n")

    print(f"{CYAN}// Copie este loop para o seu Main:{END}")
    print(f"byte xKey = 0x{key:02x};")
    print("for (int i = 0; i < buf.Length; i++) {")
    print("    buf[i] = (byte)(buf[i] ^ xKey);")
    print("}")
    print(f"\n{CYAN}{BOLD}{'='*50}{END}")

if __name__ == "__main__":
    generate_csharp_xored()
