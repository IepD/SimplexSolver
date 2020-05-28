﻿using System;
using System.Collections.Generic;
using System.Linq;
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
            if(!simplex.Minimizar)
                if (!PreValidateFields(matrizNumerica))
                    RedirectToAction("Index", "Home");
            Run();
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
            for (int i =0; i < qtdColunas; i++)
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
                    for(int i = numeroRestricoes; i < (numeroRestricoes + qtdColunas); i++)
                    {
                        aux.Elementos[i] = ElementosOriginais[i];
                    }
                    Resultado.Add(aux);
                }
            }

            if(!ExibirPassoAPasso)
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
                    Results.Add(new Result() { 
                        Elemento = quadroSimplex.Elementos[i], 
                        Valor = i < numeroRestricoes ? quadroSimplex.Matriz[i, qtdColunas - 1] : 
                        (i == numeroRestricoes + qtdColunas - 1 ? quadroSimplex.FuncaoObjetiva[qtdColunas - 1] : 0) });
            }
            ViewData["Resultado"] = Results;
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

        public bool PreValidateFields(decimal[,] matriz)
        {
            for (int i = 0; i < matriz.GetLength(0); i++)
            {
                for (int j = 0; j < matriz.GetLength(1); j++)
                {
                    if (!(matriz[i, j] >= 0))
                        throw new Exception("Valores devem ser maiores ou iguais a zero");
                }
            }

            return true;
        }

    }
}