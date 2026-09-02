# Guia de estudo do SupportFlow

Este arquivo ajuda você a entender o projeto antes de apresentá-lo no currículo ou em entrevistas.

## 1. O problema resolvido

O SupportFlow organiza solicitações de suporte. Um cliente abre um chamado, um agente assume o atendimento, altera o status e conversa por comentários. O administrador possui acesso total.

## 2. O que estudar em cada pasta

### Models

Representam os dados e as regras centrais. A classe `Ticket` impede mudanças de status inválidas e atualiza sua versão a cada alteração.

### Dtos

São os objetos recebidos e devolvidos pela API. Eles evitam que as entidades do banco sejam expostas diretamente e permitem aplicar validações.

### Data

O `AppDbContext` configura o Entity Framework Core, relacionamentos, índices e limites dos campos. O `DatabaseSeeder` cria usuários e dados de demonstração.

### Services

O `TokenService` cria tokens JWT contendo identificador, e-mail e perfil do usuário.

### Controllers

Recebem as requisições HTTP, verificam permissões, consultam o banco e devolvem respostas apropriadas.

## 3. Conceitos que você precisa explicar

1. **Injeção de dependência:** o ASP.NET entrega o `AppDbContext` e o `ITokenService` aos controllers.
2. **ORM:** o Entity Framework Core transforma consultas LINQ em comandos SQL.
3. **JWT:** depois do login, o cliente envia um token no cabeçalho `Authorization`.
4. **Autenticação:** confirma quem é o usuário.
5. **Autorização:** define o que cada perfil pode fazer.
6. **DTO:** contrato de entrada ou saída separado da entidade do banco.
7. **Paginação:** evita devolver todos os registros em uma única resposta.
8. **Concorrência otimista:** a versão impede que uma atualização antiga sobrescreva outra mais recente.
9. **Teste unitário:** valida uma regra isolada, sem depender do banco ou da internet.
10. **CI:** o GitHub Actions compila e testa o código automaticamente.

## 4. Roteiro para demonstrar o sistema

1. Abra o Swagger.
2. Registre um cliente.
3. Faça login e copie o token.
4. Clique em `Authorize` e informe o token.
5. Crie um chamado.
6. Entre como Agent e liste todos os chamados.
7. Atribua o chamado ao agente.
8. Mude de `Open` para `InProgress` e depois para `Resolved`.
9. Adicione comentários com os dois usuários.
10. Execute os testes e mostre o resultado.

## 5. Como descrever no currículo

**SupportFlow API - Sistema de gestão de chamados**

- Desenvolvimento de API REST com C# e ASP.NET Core para cadastro, acompanhamento e atribuição de chamados.
- Implementação de autenticação JWT e autorização baseada nos perfis Customer, Agent e Admin.
- Persistência com Entity Framework Core e SQLite, incluindo filtros, paginação e controle de concorrência.
- Documentação com Swagger, testes unitários com xUnit, containerização com Docker e CI com GitHub Actions.

## 6. Perguntas prováveis em entrevista

### Por que você escolheu JWT?

Porque a API não precisa manter uma sessão no servidor. O token assinado identifica o usuário e carrega suas permissões.

### Por que utilizar DTOs?

Para controlar os dados aceitos e retornados, aplicar validações e evitar o acoplamento entre os contratos HTTP e o banco.

### Por que SQLite?

Para tornar a demonstração simples. Em uma implantação real, a aplicação pode migrar para PostgreSQL ou SQL Server.

### Onde está a regra de negócio mais importante?

No método `ChangeStatus` da entidade `Ticket`, que controla as transições permitidas e impede alterações depois do fechamento.

### O que você melhoraria para produção?

Usaria migrations, segredos externos, PostgreSQL ou SQL Server, logs estruturados, testes de integração e refresh tokens.
