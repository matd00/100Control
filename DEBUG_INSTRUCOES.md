# Instruções para Debugging - Crash ao Entrar em Pedidos

## Como Capturar os Logs de Debug

### Opção 1: Executar pelo Visual Studio (RECOMENDADO)

1. **Abra o projeto no Visual Studio**
2. **Abra a janela "Output" (Saída)**:
   - Menu: `View` → `Output` (ou pressione `Ctrl+Alt+O`)
   - Na janela Output, selecione "Debug" no dropdown "Show output from:"

3. **Execute a aplicação em modo Debug**:
   - Pressione `F5` ou clique em "Start Debugging"

4. **Reproduza o erro**:
   - Quando a aplicação abrir, clique no menu "Pedidos"
   - O crash deve ocorrer

5. **Copie os logs**:
   - Na janela Output, você verá logs detalhados começando com:
     ```
     ╔════════════════════════════════════════════════════════════╗
     ║  CLIQUE NO MENU PEDIDOS - INICIANDO CARREGAMENTO           ║
     ╚════════════════════════════════════════════════════════════╝
     ```
   - Copie TODOS os logs que aparecerem
   - Procure especialmente por linhas que começam com `!!!` (indicam erros)

### Opção 2: Executar pelo Terminal com DebugView

1. **Baixe o DebugView** (opcional, mas recomendado):
   - Download: https://learn.microsoft.com/en-us/sysinternals/downloads/debugview
   - Execute o DebugView como Administrador

2. **Execute a aplicação**:
   ```powershell
   cd src\Desktop
   dotnet run
   ```

3. **No DebugView**:
   - Você verá todos os logs em tempo real
   - Capture os logs quando o crash ocorrer

### Opção 3: Ver logs após o crash

Se a aplicação crashar antes de você poder ver os logs:

1. **No Visual Studio**:
   - Vá em `Debug` → `Windows` → `Exception Settings`
   - Marque "Common Language Runtime Exceptions"
   - Execute novamente (F5)
   - Quando o erro ocorrer, o Visual Studio pausará exatamente no ponto do erro

## O que Procurar nos Logs

Os logs seguem esta estrutura:

```
=== APP STARTUP: Iniciando aplicação ===
APP STARTUP: Configurando serviços...
APP STARTUP: Serviços configurados com sucesso
...

╔════════════════════════════════════════════════════════════╗
║  CLIQUE NO MENU PEDIDOS - INICIANDO CARREGAMENTO           ║
╚════════════════════════════════════════════════════════════╝

=== LOADVIEW: Carregando OrdersLayoutView com OrdersLayoutViewModel ===
=== OrdersLayoutViewModel: Iniciando construtor ===
=== OrdersViewModel: Iniciando construtor ===
OrdersViewModel: Validando dependências...
  ✓ orderRepository OK
  ✓ customerRepository OK
  ...
```

### Marcadores Importantes:

- `✓` = Operação bem-sucedida
- `>>>` = Início de operação assíncrona
- `!!!` = **ERRO CRÍTICO** (procure por estas linhas!)
- `!!` = Erro em operação específica

## Informações Necessárias

Por favor, me envie:

1. **Todos os logs** desde `APP STARTUP` até o crash
2. **Especialmente** todas as linhas que começam com `!!!`
3. **O tipo de exceção** (ex: NullReferenceException, InvalidOperationException)
4. **A mensagem de erro completa**
5. **O StackTrace** (se disponível)

## Exemplo do que estamos procurando:

```
!!! ERRO CRÍTICO no OrdersViewModel construtor: InvalidOperationException
!!! Mensagem: Unable to resolve service for type 'SuperFreteSettings'
!!! StackTrace: at Microsoft.Extensions.DependencyInjection...
```

Isso me dirá exatamente qual dependência está faltando ou qual operação está falhando.

## Próximos Passos Após Capturar os Logs

Com os logs, poderei:
1. Identificar exatamente qual dependência está faltando
2. Identificar se é um problema de configuração
3. Identificar se há um problema com o banco de dados
4. Implementar a correção específica

---

**IMPORTANTE**: Execute a aplicação em modo Debug (F5 no Visual Studio) para capturar todos os logs!
