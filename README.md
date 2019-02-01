# CommonLogicNodes
Common LogicNodes for Gira X1/L1 Beta-SDK

## Nodes
Short description of nodes. Including state (tested hardware, test coverage, etc.)

### DewPoint
Dew point calculator.

Tested environment: GPA, X1

Dev: Alram Lechner

Todo:
- increase test coverage

### SendMail
Sends mail via SMTP.
Known to work with:
- Synology DSM 6.x, no encryption, no authentication
- GMX, encryption=STARTTLS (port 587), user+password authentication

Status: in development

Tested environment: GPA, X1

Dev: Alram Lechner

Todo:
- add feature params for subject, mailtext
- add feature ssl/tls support
- add feature authentication
- increase test coverage

