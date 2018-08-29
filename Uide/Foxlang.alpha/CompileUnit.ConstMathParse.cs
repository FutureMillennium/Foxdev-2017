using System.Collections.Generic;
using static FoxlangAlpha.FoxlangCompiler;

namespace FoxlangAlpha
{
	partial class CompileUnit
	{
		bool ConstMathParse(MathEl curEl)
		{
			bool canEnd = false;

			Stack<MathEl> stack = new Stack<MathEl>();
			stack.Push(curEl);

			uint DoOp(uint left, char op, uint right)
			{
				switch (op)
				{
					case '+':
						left += right;
						break;
					case '-':
						left -= right;
						break;
				}
				return left;
			}

			bool DoVal(uint val)
			{
				if (curEl.op != 0 && curEl.val != null)
					curEl.val = DoOp((uint)curEl.val, curEl.op, val);
				else if (curEl.val == null)
					curEl.val = val;
				else
					return AddError("Error: This shouldn't happen.");

				if (stack.Count == 1)
					canEnd = true;

				return true;
			}


			while (i < iMax)
			{
				uint newVal;

				switch (tokens[i].token)
				{
					case "+":
					case "-":
						curEl.op = tokens[i].token[0];
						canEnd = false;
						break;
					case ";":
						if (canEnd)
						{
							return true;
						}
						else
							return AddError("Unexpected ';'.");
					case "(":
						if ((curEl.op == 0 && curEl.val == null)
							|| curEl.val != null)
						{
							curEl = new MathEl();
							stack.Push(curEl);
							canEnd = false;
						}
						else
							return AddError("Unexpected '('.");
						break;
					case ")":
						if (stack.Count > 1)
						{
							stack.Pop();
							MathEl prevEl = stack.Peek();
							if (prevEl.op != 0)
								prevEl.val = DoOp((uint)prevEl.val, prevEl.op, (uint)curEl.val);
							else
								prevEl.val = curEl.val;

							curEl = prevEl;

							if (stack.Count == 1)
								canEnd = true;
						}
						else if (canEnd)
						{
							return true;
						}
						else
							return AddError("Unexpected ')'.");
						break;
					default:
						if (canEnd)
						{
							return true;
						}
						else if (ParseLiteral(tokens[i].token, out newVal))
						{
							if (DoVal(newVal) == false)
								return false;
						}
						else
						{
							Var foundVar;
							if (FindVar(MakeNamespace(tokens[i].token), out foundVar, consts) == false)
								return AddError("Can't parse this literal, or undefined constant: " + tokens[i].token);

							if (DoVal((uint)foundVar.value) == false)
								return false;

							canEnd = true;
						}
						break;
				}

				i++;
			}

			return false;
		}
	}
}
