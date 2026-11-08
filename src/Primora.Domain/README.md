# Domínio Primora

Tenant = unidade. As nove entidades ficam em `Entities/`, com IDs textuais da origem e validações de integridade. Versões, unidades e clientes delimitam as chaves compostas; a persistência é configurada exclusivamente na Infrastructure.

O domínio não depende de EF Core, HTTP ou infraestrutura. Valores monetários e proporcionais usam decimal. Eventos preservam DateTimeKind.Unspecified; suas posições de origem distinguem ocorrências no mesmo horário. As séries de unidade preservam posições, sem datas deduzidas.

Não há importação, autenticação, fórmulas analíticas novas nem criação artificial de cadastros para completar os agregados.
