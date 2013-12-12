CntlmUI
=======
Description
-----------

<i>"Cntlm ([user-friendly wiki](http://sf.net/apps/mediawiki/cntlm/) / [technical manual](http://cntlm.sourceforge.net/cntlm_manual.pdf)) is an NTLM / NTLM Session Response / NTLMv2 authenticating HTTP proxy intended to help you break free from the chains of Microsoft proprietary world. You can use a free OS and honor our noble idea, but you can’t hide. Once you’re behind those cold steel bars of a corporate proxy server requiring NTLM authentication, you’re done with. The same even applies to 3rd party Windows applications, which don’t support NTLM natively.</i>

<i>Here comes Cntlm. It stands between your applications and the corporate proxy, adding NTLM authentication on-the-fly. You can specify several “parent” proxies and Cntlm will try one after another until one works. All auth’d connections are cached and reused to achieve high efficiency. Just point your apps proxy settings at Cntlm, fill in cntlm.conf (cntlm.ini) and you’re ready to do. This is useful on Windows, but essential for non-Microsoft OS’s."</i> – taken from the [projects website](http://cntlm.sourceforge.net/).

I was in the same situation: trapped behind a corporate proxy server with the convenience of automated NTLM authentication. No problems so far, as I’m using Windows 7. Someday I wanted to install Debian 6 in a virtual machine. It was a net-install ISO and of course it denied a collaboration with our companies proxy. After some googling I found the command line utility cntlm, which offered exactly what I was looking for. But wait, I’m a Windows user and using it via CMD or in a batch file just isn’t my style :-D So I hacked a little tool called CntlmUI (as always, my projects names are creative as hell, aren’t they?) which acts as a front-end for the Cntlm binary. Features implemented so far:

Features
--------
* auto detection of the current users proxy settings
* auto detection of the current sessions user name and domain
* automated hashing of the users password
* hidden starting up
* auto start with windows
* auto connect on launch
* choose between NTLM, LM and NT authentication mode

A "ready-to-use" archiv is located in the Download section, Cntlm binaries are included.

For more information visit http://cntlm.sourceforge.net/
