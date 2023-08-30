using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MathNet.Symbolics;
using static MathNet.Symbolics.VisualExpression;
using Expr = MathNet.Symbolics.Expression;

namespace Calculadora
{
    enum KeyPanelType { Numeric, Operation, Del, Equal, Point, Invalid, Brackets, SquareRoot}
    internal class KeyPanel : INotifyPropertyChanged
    {
        public enum EOperators { Sum = '+', Substract = '-', Multiply = 'x', Divide = '÷'}
        private char[] delimiters = { (char)EOperators.Sum, (char)EOperators.Substract, (char)EOperators.Multiply, (char)EOperators.Divide };
        private string screenExpression = string.Empty;
        private string screenResult = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public static List<string>? SeparateForTerms(string expression)
        {
            List<string>? termsSeparated;
            string term;

            termsSeparated = null;
            if (!string.IsNullOrEmpty(expression))
            {
                termsSeparated = new List<string>();
                term = string.Empty;
                foreach (char value in expression)
                {
                    if (!IsOperationKey(value) || term.Count(element => element == '(') > term.Count(element => element == ')'))
                    {
                        term += value;
                    }
                    else
                    {
                        termsSeparated.Add(term);
                        term = string.Empty;
                    }
                }
                if(!string.IsNullOrEmpty(term)) termsSeparated.Add(term);
            }
            return termsSeparated;
        }
        public static bool getBeginAndLastIndexFromIndex(int indexStart, string str, out int beginIndex,out int lastIndex)
        {
            List<string>? termsSeparated;
            string mathTerm;
            int indexTerm;
            bool ok;

            termsSeparated = SeparateForTerms(str);
            beginIndex = -1;
            lastIndex = -1;
            indexTerm = 0;
            ok = false;

            if (termsSeparated != null && indexStart > -1 && indexStart < str.Length && termsSeparated.Count > 0) {
                while (lastIndex < indexStart)
                {
                    mathTerm = termsSeparated[indexTerm];
                    if (lastIndex > 0)
                    {
                        lastIndex++;
                    }
                    lastIndex += mathTerm.Length;
                    beginIndex = lastIndex - mathTerm.Length;
                    indexTerm++;
                    ok = true;
                }
            }
            beginIndex++;
            lastIndex++;

            return ok;
        }
        private static string getTermFromIndex(int startIndex, string expression)
        {
            string term;

            term = string.Empty;
            //if(getBeginAndLastIndexFromIndex(startIndex,expression,out int beginIndex,out int lastIndex))
            //{
            //    term = expression[beginIndex..lastIndex];
            //}
            if(getTermFromIndexTest(startIndex,expression,out int beginIndex,out int lastIndex))
            {
                term = expression[beginIndex..lastIndex];
            }
            return term;
        }
        private static string getNumBetweenOperatoinsFromIndex(int indexStart, string expression)
        {
            char[] delimiters = { (char)EOperators.Sum, (char)EOperators.Substract, (char)EOperators.Multiply, (char)EOperators.Divide };
            string numBetweenOperations;
            char valueExpression;
            int actualIndex;
            numBetweenOperations = string.Empty;

            if (!string.IsNullOrEmpty(expression) && indexStart > -1 && indexStart < expression.Length)
            {
                actualIndex = expression.LastIndexOfAny(delimiters,indexStart);
                actualIndex++;
                valueExpression = expression[actualIndex];
                while (!IsOperationKey(valueExpression) && actualIndex < expression.Length)
                {
                    valueExpression = expression[actualIndex];
                    if (IsNumericKey(valueExpression)) numBetweenOperations += valueExpression;
                    actualIndex++;
                }
            }

            return numBetweenOperations;
        }
        private static string getNumTermFromIndex(int indexStart, string expression, bool advanceBrackets = true)
        {
            char[] delimiters = { (char)EOperators.Sum, (char)EOperators.Substract, (char)EOperators.Multiply, (char)EOperators.Divide };
            int finalIndex;
            int beginIndex;
            string numReturn;
            string term;
            string patternOnlyNums;
            Match matchNumber;

            patternOnlyNums = @"(\d+|\(\d+\)|\((?>(?!√)[^()]+|\((?<depth>)|\)(?<-depth>))*(?(depth)(?!))\))";
            numReturn = string.Empty;
            if (!string.IsNullOrEmpty(expression) && indexStart >= 0  && indexStart <= expression.Length) {

                if (advanceBrackets && !string.IsNullOrEmpty(term = getTermFromIndex(indexStart, expression))) {

                    matchNumber = Regex.Match(term, patternOnlyNums);

                    numReturn = matchNumber.Value;
                }
                else {
                    if (indexStart == expression.Length) indexStart--;

                    finalIndex = expression.IndexOfAny(delimiters, indexStart);
                    if (finalIndex == -1) finalIndex = expression.Length;


                    if (indexStart == finalIndex) indexStart--;
                    beginIndex = expression.LastIndexOfAny(delimiters, indexStart);
                    if (beginIndex == -1) beginIndex = 0;

                    numReturn = expression[beginIndex..finalIndex];
                }
            }
            return numReturn;
        }
        public static bool getTermFromIndexTest(int startIndex, string expression, out int beginIndex, out int lastIndex)
        {
            int actualIndex;
            int countBracketOpen;
            int countBracketClose;
            bool? bracketsCompleted;
            bool ok;
            char elementExpression;

            beginIndex = -1;
            lastIndex = -1;
            actualIndex = startIndex;
            countBracketOpen = 0;
            countBracketClose = 0;
            ok = false;
            bracketsCompleted = null;

            if (!string.IsNullOrEmpty(expression) && startIndex > -1 && startIndex < expression.Length) {
                do
                {
                    elementExpression = expression[actualIndex];

                    if (IsOperationKey(elementExpression))
                    {
                        if (bracketsCompleted == false)
                        {
                            if (actualIndex < expression.Length-1) actualIndex++;
                        }

                        else actualIndex--;
                    }
                    if (isBracketsKey(elementExpression))
                    {
                        bracketsCompleted = false;
                        if (elementExpression == '(') countBracketOpen++;
                        else
                        {
                            countBracketClose++;
                            if (countBracketOpen == countBracketClose)
                            {
                                actualIndex++;
                                break;
                            }
                            if (countBracketOpen < countBracketClose)
                            {
                                bracketsCompleted = null;
                                actualIndex--;
                            }
                        }
                    }
                    actualIndex++;

                } while (actualIndex < expression.Length &&
                !((IsOperationKey(elementExpression) || isBracketsKey(elementExpression)) && bracketsCompleted == null));

                ok = true;
                beginIndex = startIndex;
                lastIndex = actualIndex;
            }
            return ok;
        }
        public static string replaceTermFromIndex(int indexStart, string expression, string newTerm)
        {
            string operationReturn;
            operationReturn = string.Empty;

            //if (getBeginAndLastIndexFromIndex(indexStart, expression, out int beginIndex, out int lastIndex))
            //{
            if (getTermFromIndexTest(indexStart, expression, out int beginIndex, out int lastIndex))
            {
                //string jose = expression[beginIndex..lastIndex];
                //if (jose.Count(item => item == '√') > 1)
                //{
                //    beginIndex++;
                //    lastIndex--;
                //    getBeginAndLastIndexFromIndex(beginIndex, expression[beginIndex..lastIndex], out beginIndex, out lastIndex);
                //}
                //jose = jose[beginIndex..lastIndex];
                operationReturn = expression.Substring(0, beginIndex) + newTerm + expression.Substring(lastIndex);
            }

            return operationReturn;
        }
        public static string replaceAllTermsWithNums(string expression, string oldTerm, string newTerm, string newTermClose = "")
        {
            string newExpression;
            string numsExpression;
            int indexSearch;

            newExpression = expression;
            indexSearch = newExpression.IndexOf(oldTerm);
            while (indexSearch != -1)
            {
                //numsExpression = getNumBetweenOperatoinsFromIndex(indexSearch, newExpression);
                numsExpression = getNumTermFromIndex(indexSearch, newExpression);

                if (!string.IsNullOrEmpty(numsExpression))
                {
                    newExpression = replaceTermFromIndex(indexSearch, newExpression, newTerm+numsExpression+newTermClose);
                }
                indexSearch = newExpression.IndexOf(oldTerm, indexSearch + 1);
            }

            return newExpression;
        }
        public static string standarizeExpression(string expression)
        {
            string standarizedExpression;

            standarizedExpression = string.Empty;
            if (!string.IsNullOrEmpty(expression))
            {
                standarizedExpression = expression;

                standarizedExpression = replaceAllTermsWithNums(standarizedExpression, "√","sqrt(",")");
                standarizedExpression = standarizedExpression.Replace("÷", "/");
                standarizedExpression = standarizedExpression.Replace("x", "*");
                standarizedExpression = standarizedExpression.Replace(",", ".");
            }

            return standarizedExpression;
        }
        public static char standarizeOperation(Key key)
        {
            char standarizedOperation;
            standarizedOperation = (char)KeyInterop.VirtualKeyFromKey(key);
            standarizedOperation = char.ToLower(standarizedOperation);

                if (key == Key.Back) standarizedOperation = ' ';
                if (key == Key.OemPlus) standarizedOperation = '+';
                if (key == Key.OemMinus) standarizedOperation = '-';
                if (key == Key.OemPeriod) standarizedOperation = '.';
                if (key == Key.Enter) standarizedOperation = '=';

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (key == Key.D7) standarizedOperation = (char)EOperators.Divide;
                    if (key == Key.OemPlus) standarizedOperation = (char)EOperators.Multiply;
                    if (key == Key.D8) standarizedOperation = '(';
                    if (key == Key.D9) standarizedOperation = ')';
                }
            return standarizedOperation;
        }
        public bool ValidateInsert(char key, int index) {
            bool ok;
            string[] numbersAux;
            ok = true;

            switch (TypeOfKey(key))
            {
                case KeyPanelType.Invalid: ok = false; break;

                case KeyPanelType.Operation:
                    {
                        if (index == 0 && key != '-') ok = false;

                        if (index > -1 && index < ScreenExpression.Length)
                        {
                            if (IsOperationKey(ScreenExpression[index])) ok = false;
                        }
                        if ((index - 1) > -1 && (index - 1) < ScreenExpression.Length)
                        {
                            if (IsOperationKey(ScreenExpression[index - 1])) ok = false;
                        }
                    }break;

                case KeyPanelType.Point:
                    {
                        if (getNumTermFromIndex(index,ScreenExpression,false).Contains('.')) ok = false;
                    }break;

                case KeyPanelType.Brackets:
                    {
                        if (key == ')')
                        {
                            if (ScreenExpression.IndexOf('(', 0) == -1 || ScreenExpression[index - 1] == '(') ok = false; //Si NO contiene un '(' o si tiene un '(' al lado izquierdo

                            else
                            {
                                for (int i = index - 1; ScreenExpression[i] != '(' && i > -1; i--)
                                {
                                    ok = false;
                                    if (IsNumericKey(ScreenExpression[i]))
                                    {
                                        ok = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }break;

                case KeyPanelType.Del:
                    {
                        if (index < 1) ok = false;
                    }break;

                case KeyPanelType.Equal:
                    {
                        if (!double.TryParse(ScreenResult, out double numDouble)) ok = false;
                        else
                        {
                            numbersAux = ScreenExpression.Split(delimiters);
                            if (numbersAux.Length < 2) ok = false;

                            else {
                                if (string.IsNullOrEmpty(numbersAux[1]) || double.IsInfinity(numDouble) || double.IsNaN(numDouble)) ok = false;
                            } 
                        }
                    }
                    break;
            }
            
            return ok;
        }
        public bool Insert(char key, int index, out int indexToSelected)
        {
            bool ok;
            double? resultOperation;
            string operations;
            KeyPanelType keyType;

            ok = false;
            indexToSelected = index;
            operations = string.Empty;
            keyType = TypeOfKey(key);

            if (!ValidateInsert(key, index)) return ok;
            
            switch (keyType)
            {
                case KeyPanelType.Invalid: return ok;

                case KeyPanelType.Equal:
                    {
                        ScreenExpression = ScreenResult;
                        clearScreenResult();
                    }break;

                case KeyPanelType.Del:
                    {
                        ScreenExpression = ScreenExpression.Remove(index - 1, 1);
                        indexToSelected = index - 1;
                    }break;

                default:
                    {
                        ScreenExpression = ScreenExpression.Insert(index, key.ToString());
                        indexToSelected = index + 1;
                    }
                    break;
            }
            ok = true;
            operations = ScreenExpression;
            resultOperation = DoOperation(operations);

            if (!string.IsNullOrEmpty(resultOperation.ToString())) ScreenResult = resultOperation.ToString();

            return ok;
        }

        public void clearScreenResult()
        {

            ScreenResult = string.Empty;
        }
        public static bool IsNumericKey(char key) => int.TryParse(key.ToString(), out _);
        public static bool IsOperationKey(char key) => key == '+' || key == '-' || key == 'x' || key == '*' || key == '÷' || key == '/';
        public static bool isDelKey(char key) => key == ' ';
        public static bool isEqualKey(char key) => key == '=';
        public static bool isPointKey(char key) => key == '.';
        public static bool isBracketsKey(char key) => key == '(' || key == ')';
        public static bool isSquareRootKey(char key) => key == '√';

        public static KeyPanelType TypeOfKey(char key)
        {
            KeyPanelType keyPanelTypeReturn;
            keyPanelTypeReturn = KeyPanelType.Invalid;

            if (IsNumericKey(key)) return KeyPanelType.Numeric;

            if (IsOperationKey(key)) return KeyPanelType.Operation;

            if (isDelKey(key)) return KeyPanelType.Del;

            if (isEqualKey(key)) return KeyPanelType.Equal;

            if (isPointKey(key)) return KeyPanelType.Point;

            if (isBracketsKey(key)) return KeyPanelType.Brackets;

            if (isSquareRootKey(key)) return KeyPanelType.SquareRoot;

            return keyPanelTypeReturn;
        }
 

        public static double? DoOperation(string? operations)
        {
            FloatingPoint evaluateExpr;
            Expr mathExpr;
            double? resultOperation;

            resultOperation = null;
            if (!string.IsNullOrEmpty(operations))
            {
                operations = standarizeExpression(operations);

                try
                {
                    mathExpr = Infix.ParseOrThrow(operations);
                    evaluateExpr = Evaluate.Evaluate(null,mathExpr);

                    if (evaluateExpr != null)
                    {
                        if (evaluateExpr.IsReal) resultOperation = evaluateExpr.RealValue;

                        else if (evaluateExpr.IsComplexInf) resultOperation = double.PositiveInfinity;

                        else if (evaluateExpr.IsUndef) resultOperation = double.NaN;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("OPERATION ERROR");
                }
            }

            return resultOperation;
        }

        public string ScreenExpression
        {
            get { return this.screenExpression; }
            set
            {
                this.screenExpression = value;
                OnPropertyChanged(nameof(ScreenExpression));
            }
        }
        public string ScreenResult
        {
            get { return this.screenResult; }
            set
            {
                string? valueStandarized;
                valueStandarized = standarizeExpression(value);

                while (valueStandarized.Length > 16)
                {
                    valueStandarized = valueStandarized.Remove(valueStandarized.Length - 1);
                }

                this.screenResult = valueStandarized;
                OnPropertyChanged(nameof(ScreenResult));
            }
        }
    }
}
