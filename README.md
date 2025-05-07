# Cloudflare DDNS Updater

### Client per l'aggiornamento dei record A Cloudflare

- Ottiene l'indirizzo IP da 'https://ipv4.icanhazip.com/' o 'https://api.ipify.org'
- Ottiene automaticamente lo zone_id e record_id usando il nome del dominio
- Aggiorna tutti i record di tipo A sul dominio

#### Configurazione

Il file di configurazione viene generato dopo il primo avvio nella directory dell'eseguibile
Modificare config.json nella directory di installazione (default:'C:\Program Files (x86)\CloudflareDDNS-Updater-Setup'):

  -   "ApiKey": "YOUR_API_KEY",
  -   "Email": "YOUR_EMAIL",
  -   "Domain": "yourdomain.com"

.Net 8
