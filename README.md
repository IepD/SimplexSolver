# SimplexSolver

Repositório para a implementação do algoritmo Simplex.

Projeto de Pesquisa Operacional
5º Semestre BCC UNIVEM

* Everton Martins dos Santos (57765-0)
* Gabriel Fernando de Carvalho Silva (58196-8)
* José Renato Garcia Martinez Martins  (57928-9)
* Lucas Felipe de Oliveira Dias (58138-0)

O Simplex permite que se encontre valores ideais em situações em que diversos aspectos precisam ser respeitados. Diante de um problema, são estabelecidas inequações que representam restrições para as variáveis. A partir daí, testa-se possibilidades de maneira a otimizar, isto é, maximizar ou minimizar o resultado da forma mais rápida possível.

## Ferramentas

- Javascript
- JQuery
- Bootstrap
- GitHub para versionamento
- C#

### Simplex

- Algoritmo Simplex para problemas de maximização.
- Algoritmo Simplex para problemas de minimização.
- É exibido o passo a passo das tabelas geradas pelo método Simplex
- Tabela de Sensibilidade.

## Entradas personalizadas para:

### Simplex

- Limite máximo de iterações
- Tipo de Simplex (MAX ou MIN)
- Quantidade de variáveis e restrições

## Limitações

### Simplex

- Em cada variável da função objetivo e das restrições deve conter apenas o número, sem a adição do 'x' e caso tenha alguma variável nula, é necessário inserir o 0.

## Datas Importantes

### Simplex

Datas | Eventos
--------- | ------
26/05/20     | Simplex max e Restrições <=
26/05/20     | Permitir múltiplas variáveis de decisão e restrições.
26/05/20     | Apresentar resultado final.
02/06/20     | Simplex Min **(MVP)**
02/06/20     | Quadros com soluções parciais **(MVP)**
09/06/20     | Analise de Sensibilidade **(MVP)**
09/06/20     | Tratar solução impossível **(MVP)**
