using System;
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
        List<QuadroSimplex> Resultado;
        bool ExibirPassoAPasso;
        #endregion

        public IActionResult Index(Simplex simplex)
        {
            simplex.Variaveis = Convert.ToInt32(TempData["Variaveis"]);
            simplex.Restricoes = Convert.ToInt32(TempData["Restricoes"]);
            simplex.Minimizar = Convert.ToBoolean(TempData["Minimizar"]);
            ExibirPassoAPasso = Convert.ToBoolean(TempData["ExibirPassoAPasso"]);
            PrepareToExecute(simplex);
            Run();
            return View(Resultado);
        }

        public void PrepareToExecute(Simplex simplex)
        {
            numeroVariaveis = simplex.Variaveis.Value;
            numeroRestricoes = simplex.Restricoes.Value;
            qtdColunas = (numeroVariaveis * 2) + 1;
            funcaoObjetiva = new decimal[qtdColunas];
            matrizNumerica = new decimal[numeroRestricoes, qtdColunas];
            Var = new string[numeroRestricoes + qtdColunas];

            for (int i =0; i < qtdColunas; i++)
            {
                if (i < numeroVariaveis)
                    funcaoObjetiva[i] = (simplex.Minimizar ? simplex.objectiveVector[i] : -simplex.objectiveVector[i]);
                else
                    funcaoObjetiva[i] = 0;
            }

            for(int i = 0, k = 0; i < numeroRestricoes; i++)
            {
                for (int j = 0; j < numeroVariaveis + 1; j++)
                {
                    if (j == numeroVariaveis)
                        j = (numeroVariaveis * 2);
                    matrizNumerica[i, j] = simplex.Matriz[k];
                    k++;
                }
            }

            for (int i = 0, j = 1; i < (numeroRestricoes + qtdColunas); i++, j++)
            {
                if (j > numeroVariaveis)
                {
                    j = 1;
                }
                if (i < numeroRestricoes)
                    Var[i] = "F" + j;
                else if (i < qtdColunas - 1)
                    Var[i] = "X" + j;
                else if (i < (numeroRestricoes + qtdColunas - 1))
                    Var[i] = "F" + j;
                else
                    Var[i] = "B";
            }
            quadroSimplex = new QuadroSimplex(Var, matrizNumerica, funcaoObjetiva);

        }

        public void Run()
        {
            Resultado = new List<QuadroSimplex>();
            if(ExibirPassoAPasso)
            {
                QuadroSimplex inicial = quadroSimplex.Clone(quadroSimplex);
                Resultado.Add(inicial);
            }
            while (quadroSimplex.FuncaoObjetiva.Where(x => x < 0).Any() && qtdInterations < 100)
            {
                qtdInterations++;
                decimal lowestValueToOut = 0;
                int positionOutLine = 0;

                int positionEnteringElement;
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

                var Element = quadroSimplex.Elementos[quadroSimplex.Matriz.GetLength(0) + positionEnteringElement];
                quadroSimplex.Elementos[quadroSimplex.Matriz.GetLength(0) + positionEnteringElement] = quadroSimplex.Elementos[positionOutLine];
                quadroSimplex.Elementos[positionOutLine] = Element;

                decimal valueLoadLine = -quadroSimplex.FuncaoObjetiva[positionEnteringElement];

                for (int i = 0; i < quadroSimplex.Matriz.GetLength(1); i++)
                    quadroSimplex.FuncaoObjetiva[i] = quadroSimplex.Matriz[positionOutLine, i] * valueLoadLine + quadroSimplex.FuncaoObjetiva[i];

                if (ExibirPassoAPasso)
                {
                    QuadroSimplex aux = quadroSimplex.Clone(quadroSimplex);
                    Resultado.Add(aux);
                }  
            }
            if(!ExibirPassoAPasso)
            {
                QuadroSimplex final = quadroSimplex.Clone(quadroSimplex);
                Resultado.Add(final);
            }
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

    }
}