using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimplexSolver.Models;

namespace SimplexSolver.Controllers
{
    public class SimplexController : Controller
    {
        #region "Variáveis"
        QuadroSimplex quadroSimplex;
        int numeroVariaveis;
        int numeroRestricoes;
        int qtdColunas;
        int qtdInterations;
        decimal[] funcaoObjetiva;
        decimal[,] matrizNumerica;
        QuadroSimplex quadroSimplexOriginal;
        string[] Var;
        string[] ElementosOriginais;
        List<QuadroSimplex> Resultado;
        bool ExibirPassoAPasso;
        bool Minimizar;
        List<Result> Results;
        #endregion

        public IActionResult Index(Simplex simplex)
        {
            simplex.Variaveis = Convert.ToInt32(TempData["Variaveis"]);
            simplex.Restricoes = Convert.ToInt32(TempData["Restricoes"]);
            simplex.Minimizar = Convert.ToBoolean(TempData["Minimizar"]);
            ExibirPassoAPasso = Convert.ToBoolean(TempData["ExibirPassoAPasso"]);
            Minimizar = simplex.Minimizar;
            PrepareToExecute(simplex);
            if (!PreValidateFields(matrizNumerica, simplex.Minimizar))
                RedirectToAction("Index", "Home");
            Run();
            AnaliseSensibilidade();
            ViewData["ExibirPassoAPasso"] = ExibirPassoAPasso;
            return View(Resultado);
        }

        public void PrepareToExecute(Simplex simplex)
        {
            #region Preechimento das variáveis necessárias
            numeroVariaveis = simplex.Variaveis.Value;
            numeroRestricoes = simplex.Restricoes.Value;
            qtdColunas = (numeroVariaveis + numeroRestricoes) + 1;
            funcaoObjetiva = new decimal[qtdColunas];
            matrizNumerica = new decimal[numeroRestricoes, qtdColunas];
            Var = new string[numeroRestricoes + qtdColunas];
            #endregion

            #region Montar vetor da função objetivo
            for (int i = 0; i < qtdColunas; i++)
            {
                if (i < numeroVariaveis)
                    funcaoObjetiva[i] = (simplex.Minimizar ? simplex.objectiveVector[i] : -simplex.objectiveVector[i]);
                else
                    funcaoObjetiva[i] = 0;
            }
            #endregion

            #region Montar matriz de acordo com valores preenchidos
            for (int i = 0, k = 0; i < numeroRestricoes; i++)
            {
                for (int j = 0; j < numeroVariaveis + 1; j++)
                {
                    if (j == numeroVariaveis)
                        j = (numeroVariaveis + numeroRestricoes);
                    matrizNumerica[i, j] = simplex.Matriz[k];
                    k++;
                }
            }

            for (int i = 0; i < numeroRestricoes; i++)
            {
                matrizNumerica[i, numeroVariaveis + i] = 1;
            }
            #endregion

            #region Montar vetor com Elementos da operação
            for (int i = 0, j = 1, k = 1; i < (numeroRestricoes + qtdColunas); i++)
            {
                if (j > numeroRestricoes)
                {
                    j = 1;
                }
                if (i < numeroRestricoes)
                {
                    Var[i] = "F" + j;
                    j++;
                }
                else if (i < qtdColunas - 1)
                {
                    Var[i] = "X" + k;
                    k++;
                }
                else if (i < (numeroRestricoes + qtdColunas - 1))
                {
                    Var[i] = "F" + j;
                    j++;
                }
                else
                    Var[i] = "B";
            }
            #endregion

            ElementosOriginais = (string[])Var.Clone();

            quadroSimplex = new QuadroSimplex(Var, matrizNumerica, funcaoObjetiva);
            quadroSimplexOriginal = quadroSimplex.Clone(quadroSimplex);
        }

        public void Run()
        {
            //Inicialização da variável utilizada para exibição do resultado / passo a passo
            Resultado = new List<QuadroSimplex>();
            Results = new List<Result>();

            //Caso de exibição do passo a passo, inserir resultado antes da primeira interação
            if (ExibirPassoAPasso)
            {
                QuadroSimplex inicial = quadroSimplex.Clone(quadroSimplex);
                Resultado.Add(inicial);
            }

            while (quadroSimplex.FuncaoObjetiva.Where(x => x < 0).Any() && qtdInterations < 100)
            {
                #region Variaveis auxiliares
                qtdInterations++;
                decimal lowestValueToOut = 0;
                int positionOutLine = 0;
                int positionEnteringElement;
                #endregion


                //Buscando coluna do elemento que entrará na base
                positionEnteringElement = FindPositionLowestValue(quadroSimplex.FuncaoObjetiva);
                if (!(positionEnteringElement >= 0))
                    throw new Exception("Algo deu errado na busca da posicao");

                //Buscando linha do elemento que sairá da base
                for (int i = 0; i < quadroSimplex.Matriz.GetLength(0); i++)
                {
                    if (quadroSimplex.Matriz[i, positionEnteringElement] != 0)
                    {
                        decimal resultAux = quadroSimplex.Matriz[i, quadroSimplex.Matriz.GetLength(1) - 1] / quadroSimplex.Matriz[i, positionEnteringElement];
                        if ((resultAux < lowestValueToOut && resultAux >= 0) || lowestValueToOut == 0)
                        {
                            lowestValueToOut = resultAux;
                            positionOutLine = i;
                        }
                    }
                    else
                        continue;
                }

                //Cálculos das novas linhas/colunas
                if (!(quadroSimplex.Matriz[positionOutLine, positionEnteringElement] == 0))
                {
                    decimal pivo = quadroSimplex.Matriz[positionOutLine, positionEnteringElement];

                    for (int i = 0; i < quadroSimplex.Matriz.GetLength(1); i++)
                    {
                        quadroSimplex.Matriz[positionOutLine, i] = Convert.ToDecimal(quadroSimplex.Matriz[positionOutLine, i] / pivo);
                    }

                    for (int i = 0; i < quadroSimplex.Matriz.GetLength(0); i++)
                        if (i == positionOutLine)
                            continue;
                        else
                        {
                            decimal valoraux = -quadroSimplex.Matriz[i, positionEnteringElement];
                            for (int j = 0; j < quadroSimplex.Matriz.GetLength(1); j++)
                            {
                                quadroSimplex.Matriz[i, j] = (quadroSimplex.Matriz[positionOutLine, j] * valoraux) + quadroSimplex.Matriz[i, j];
                            }
                        }
                }

                #region Alocação do elemento para a posição correta no vetor de acordo com a entrada/saída de elementos
                var Element = quadroSimplex.Elementos[quadroSimplex.Matriz.GetLength(0) + positionEnteringElement];
                quadroSimplex.Elementos[quadroSimplex.Matriz.GetLength(0) + positionEnteringElement] = quadroSimplex.Elementos[positionOutLine];
                quadroSimplex.Elementos[positionOutLine] = Element;
                #endregion

                decimal valueLoadLine = -quadroSimplex.FuncaoObjetiva[positionEnteringElement];

                for (int i = 0; i < quadroSimplex.Matriz.GetLength(1); i++)
                    quadroSimplex.FuncaoObjetiva[i] = quadroSimplex.Matriz[positionOutLine, i] * valueLoadLine + quadroSimplex.FuncaoObjetiva[i];

                if (ExibirPassoAPasso)
                {
                    QuadroSimplex aux = quadroSimplex.Clone(quadroSimplex);
                    for (int i = numeroRestricoes; i < (numeroRestricoes + qtdColunas); i++)
                    {
                        aux.Elementos[i] = ElementosOriginais[i];
                    }
                    Resultado.Add(aux);
                }
            }

            if (!ExibirPassoAPasso)
            {
                QuadroSimplex final = quadroSimplex.Clone(quadroSimplex);
                for (int i = numeroRestricoes; i < (numeroRestricoes + qtdColunas); i++)
                {
                    final.Elementos[i] = ElementosOriginais[i];
                }
                Resultado.Add(final);
            }

            for (int i = 0; i < numeroRestricoes + qtdColunas; i++)
            {
                if (Results.Where(x => x.Elemento.Equals(quadroSimplex.Elementos[i])).Any())
                    continue;
                else
                    Results.Add(new Result()
                    {
                        Elemento = quadroSimplex.Elementos[i],
                        Valor = i < numeroRestricoes ? quadroSimplex.Matriz[i, qtdColunas - 1] :
                        (i == numeroRestricoes + qtdColunas - 1 ? (Minimizar ? -quadroSimplex.FuncaoObjetiva[qtdColunas - 1] : quadroSimplex.FuncaoObjetiva[qtdColunas - 1]) : 0)
                    });
            }

            ViewData["Resultado"] = Results;
        }

        public void AnaliseSensibilidade()
        {
            string[] ResultadoAnaliseSensibilidade = new string[(numeroRestricoes + numeroVariaveis + 1) * 14];
            int aumentarParametro = 0;
            int reduzirParametro = 0;
            for (int i = 0, k = 0; i < (numeroRestricoes + numeroVariaveis + 1); i++)
            {
                for (int j = 0; j < 14; j++, k++)
                {
                    switch (j)
                    {
                        #region Primeira Coluna
                        case 0:
                            ResultadoAnaliseSensibilidade[k] = ElementosOriginais[i + numeroRestricoes];
                            break;
                        #endregion

                        #region Segunda Coluna
                        case 1:
                            if (i < numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = "Decisão";
                            else if (i == (numeroRestricoes + numeroVariaveis))
                                ResultadoAnaliseSensibilidade[k] = "Função Objetivo";
                            else
                                ResultadoAnaliseSensibilidade[k] = "Folga";
                            break;
                        #endregion

                        #region Terceira Coluna
                        case 2:
                            if (i == (numeroRestricoes + numeroVariaveis))
                                ResultadoAnaliseSensibilidade[k] = Math.Round(quadroSimplexOriginal.FuncaoObjetiva[qtdColunas - 1], 2).ToString();
                            else
                                if (i >= numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = Math.Round(quadroSimplexOriginal.Matriz[i - numeroVariaveis, qtdColunas - 1], 2).ToString();
                            else
                                ResultadoAnaliseSensibilidade[k] = "0";
                            break;
                        #endregion

                        #region Quarta Coluna
                        case 3:
                            ResultadoAnaliseSensibilidade[k] = Math.Round(Results.Where(x => x.Elemento.Equals(ElementosOriginais[i + numeroRestricoes])).FirstOrDefault().Valor, 2).ToString();
                            break;
                        #endregion

                        #region Quinta Coluna
                        case 4:
                            bool isBasic = false;
                            for (int y = 0; y < numeroRestricoes; y++)
                            {
                                if (ElementosOriginais[i + numeroRestricoes].Equals(quadroSimplex.Elementos[y]) || i == (numeroRestricoes + numeroVariaveis))
                                {
                                    ResultadoAnaliseSensibilidade[k] = "Sim";
                                    isBasic = true;
                                    break;
                                }
                            }
                            if (!isBasic)
                                ResultadoAnaliseSensibilidade[k] = "Não";
                            break;
                        #endregion

                        #region Sexta Coluna
                        case 5:
                            if (i < numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else if (i == (numeroRestricoes + numeroVariaveis))
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else
                                ResultadoAnaliseSensibilidade[k] = (Results.Where(x => x.Elemento.Equals(ElementosOriginais[i + numeroRestricoes])).FirstOrDefault().Valor == 0 ? "Sim" : "Não");
                            break;
                        #endregion

                        #region Sétima Coluna
                        case 6:
                            if (i < numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else if (i == (numeroRestricoes + numeroVariaveis))
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else
                                ResultadoAnaliseSensibilidade[k] = Math.Round(Results.Where(x => x.Elemento.Equals(ElementosOriginais[i + numeroRestricoes])).FirstOrDefault().Valor, 2).ToString();
                            break;
                        #endregion

                        #region Oitava Coluna
                        case 7:
                            if (i < numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else if (i == (numeroRestricoes + numeroVariaveis))
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else
                                ResultadoAnaliseSensibilidade[k] = (Convert.ToDecimal(ResultadoAnaliseSensibilidade[k - 5]) - Convert.ToDecimal(ResultadoAnaliseSensibilidade[k - 4])).ToString();
                            break;
                        #endregion

                        #region Nona Coluna
                        case 8:
                            if (i < numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else if (i == (numeroRestricoes + numeroVariaveis))
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else
                                ResultadoAnaliseSensibilidade[k] = Math.Round(quadroSimplex.FuncaoObjetiva[i], 2).ToString();
                            break;
                        #endregion

                        #region Décima Coluna
                        case 9:
                            if (i < numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = Math.Round(quadroSimplex.FuncaoObjetiva[i], 2).ToString();
                            else
                                ResultadoAnaliseSensibilidade[k] = "-";
                            break;
                        #endregion

                        #region Décima Primeira Coluna
                        case 10:
                            decimal aux = -1;
                            decimal resultaux = 0;
                            if (i < numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else if (i == (numeroRestricoes + numeroVariaveis))
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else
                            {
                                for (int y = 0; y < numeroRestricoes; y++)
                                {
                                    if (quadroSimplex.Matriz[y, numeroVariaveis + aumentarParametro] == 0)
                                        continue;

                                    resultaux = (quadroSimplex.Matriz[y, qtdColunas - 1] / quadroSimplex.Matriz[y, numeroVariaveis + aumentarParametro]) * -1;

                                    if ((resultaux < aux && resultaux >= 0) || (aux == -1 && resultaux >= 0))
                                        aux = resultaux;
                                }
                                aumentarParametro++;
                                ResultadoAnaliseSensibilidade[k] = (aux != -1 ? Math.Round(aux, 2).ToString() : "INFINITO");
                            }
                            break;
                        #endregion

                        #region Décima Segunda Coluna
                        case 11:
                            decimal aux2 = 1;
                            decimal resultaux2 = 0;
                            if (i < numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else if (i == (numeroRestricoes + numeroVariaveis))
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else
                            {
                                for (int y = 0; y < numeroRestricoes; y++)
                                {
                                    if (quadroSimplex.Matriz[y, numeroVariaveis + reduzirParametro] == 0)
                                        continue;

                                    resultaux2 = (quadroSimplex.Matriz[y, qtdColunas - 1] / quadroSimplex.Matriz[y, numeroVariaveis + reduzirParametro]) * -1;

                                    if ((resultaux2 > aux2 && resultaux2 <= 0) || (aux2 == 1 && resultaux2 <= 0))
                                        aux2 = resultaux2;
                                }
                                reduzirParametro++;
                                ResultadoAnaliseSensibilidade[k] = (aux2 != 1 ? Math.Round(aux2 * -1, 2).ToString() : "INFINITO");
                            }
                            break;
                        #endregion

                        #region Décima Terceira Coluna
                        case 12:
                            if (i < numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else if (i == (numeroRestricoes + numeroVariaveis))
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else
                                if (ResultadoAnaliseSensibilidade[k - 10].Equals("INFINITO") || ResultadoAnaliseSensibilidade[k - 2].Equals("INFINITO"))
                                ResultadoAnaliseSensibilidade[k] = "INFINITO";
                            else
                                ResultadoAnaliseSensibilidade[k] = (Convert.ToDecimal(ResultadoAnaliseSensibilidade[k - 10]) + Convert.ToDecimal(ResultadoAnaliseSensibilidade[k - 2])).ToString();
                            break;
                        #endregion

                        #region Décima Quarta Coluna
                        case 13:
                            if (i < numeroVariaveis)
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else if (i == (numeroRestricoes + numeroVariaveis))
                                ResultadoAnaliseSensibilidade[k] = "-";
                            else
                                if (ResultadoAnaliseSensibilidade[k - 11].Equals("INFINITO") || ResultadoAnaliseSensibilidade[k - 2].Equals("INFINITO"))
                                ResultadoAnaliseSensibilidade[k] = "INFINITO";
                            else
                                ResultadoAnaliseSensibilidade[k] = (Convert.ToDecimal(ResultadoAnaliseSensibilidade[k - 11]) - Convert.ToDecimal(ResultadoAnaliseSensibilidade[k - 2])).ToString();
                            break;
                            #endregion
                    }
                }
            }
            ViewData["Analise"] = ResultadoAnaliseSensibilidade;
        }

        public int FindPositionLowestValue(decimal[] funcaoObjetiva)
        {
            int position = -1;
            decimal LowestValue = 0;
            for (int i = 0; i < funcaoObjetiva.Length - 1; i++)
            {
                if (funcaoObjetiva[i] < LowestValue)
                {
                    LowestValue = funcaoObjetiva[i];
                    position = i;
                }
            }
            return position;
        }

        public bool PreValidateFields(decimal[,] matriz, bool minimizar)
        {
            for (int i = 0; i < matriz.GetLength(0); i++)
            {
                for (int j = 0; j < matriz.GetLength(1); j++)
                {
                    if (!(matriz[i, j] >= 0))
                        throw new Exception("Valores devem ser maiores ou iguais a zero");
                }
            }

            for (int i = 0; i < matriz.GetLength(0); i++)
                if (!(matriz[i, matriz.GetLength(1) - 1] >= 0))
                    throw new Exception("Valores devem ser maiores ou iguais a zero");

            return true;
        }

    }
}