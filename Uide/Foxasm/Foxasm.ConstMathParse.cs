using System.Collections.Generic;

namespace Foxasm
{
	partial class Foxasm
	{
		internal class MathEl
		{
			internal uint? val = null;
			internal char op = (char)0;
		}

		int ConstMathParse(Compilers.Token next = null)
		{
			bool canEnd = false;
			MathEl curEl = new MathEl();

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
					case '*':
						left *= right;
						break;
					case '/':
						left /= right;
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
					return this.ThrowError("Error: This shouldn't happen.");

				if (stack.Count == 1)
					canEnd = true;

				return true;
			}

			int End()
			{
				iT--;
				return (int)curEl.val;
			}

			int ThrowError(string message)
			{
				this.ThrowError(message);
				return -1;
			}

			if (next == null)
				iT++;
			int iMax = tokens.Count;

			while (iT < iMax)
			{
				uint newVal;

				switch (tokens[iT].token)
				{
					case "+":
					case "-":
					case "*":
					case "/":
						curEl.op = tokens[iT].token[0];
						canEnd = false;
						break;
					case ":":
					case ";":
						if (canEnd)
						{
							return End();
						}
						else
							return ThrowError("Unexpected: " + tokens[iT].token);
					case "(":
						if ((curEl.op == 0 && curEl.val == null)
							|| curEl.val != null)
						{
							curEl = new MathEl();
							stack.Push(curEl);
							canEnd = false;
						}
						else
							return ThrowError("Unexpected '('.");
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
							return (int)curEl.val;
						}
						else
							return ThrowError("Unexpected ')'.");
						break;
					default:
						if (canEnd)
						{
							return End();
						}
						else if (tokens[iT].token == "$")
						{
							if (DoVal((uint)writer.BaseStream.Position) == false)
								return -1;
						}
						else if (TryIntLiteralParse(tokens[iT].token, out newVal))
						{
							if (DoVal(newVal) == false)
								return -1;
						}
						else if (tokens[iT].token[0] == '.')
						{
							Label found = labels.Find(x => x.symbol == tokens[iT].token);
							if (found == null)
								ThrowError("Undeclared label: “" + tokens[iT].token + "”");
							if (DoVal((uint)found.bytePos) == false)
								return -1;
						}
						else
						{
							Const found = consts.Find(x => x.token.token == tokens[iT].token);
							if (found == null)
								ThrowError("Can't parse this literal, or undefined constant: " + tokens[iT].token);
							
							if (DoVal((uint)found.val) == false)
								return -1;

							canEnd = true;
							//ThrowError("Unexpected: " + tokens[iT].token);
						}
						break;
				}

				iT++;
			}

			if (canEnd)
			{
				return End();
			}
			else
			{
				return ThrowError("Unexpected end of file.");
			}
		}
	}
}
