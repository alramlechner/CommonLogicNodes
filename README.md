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
Sends mail via SMTP. Depends on .net framework 4.5 (should be available on X1) but it is broken on X1.

Status: in development

Tested environment: GPA

Dev: Alram Lechner

Todo:
- make it work on X1 (currently it does not)
- add feature params for subject, mailtext
- add feature ssl/tls support
- add feature authentication
- increase test coverage

