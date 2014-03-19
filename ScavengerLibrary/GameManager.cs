using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScavengerLibrary
{
	public class TokenEventArgs : EventArgs
	{
		public string ID { get; set; }
	}

	public class GameStateEventArgs : EventArgs
	{
		public GameState GameState { get; set; }
	}

	public enum GameState
	{
		Off,
		Start,
		Over
	}

	public class GameManager
	{
		public event EventHandler AllTokensFound;
		public event EventHandler<TokenEventArgs> TokenFound;
		public event EventHandler<GameStateEventArgs> GameStateChange;

		object tokensLock;
		Dictionary<string, Token> tokens;
		GameState gameState;

		public GameManager ()
		{
			tokensLock = new object ();
			tokens = new Dictionary<string, Token> ();
			gameState = GameState.Off;
		}

		public void AddToken (string id)
		{
			if (!tokens.ContainsKey (id)) {
				tokens.Add (id, new Token { ID = id, Found = false });
			}
		}

		public void RemoveToken (string id)
		{
			if (tokens.ContainsKey (id)) {
				tokens.Remove (id);
			}
		}

		public void RemoveAllTokens ()
		{
			tokens.Clear ();
		}

		public void StartGame ()
		{
			gameState = GameState.Start;
			if (GameStateChange != null) {
				GameStateChange (this, new GameStateEventArgs { GameState = gameState });
			}
		}

		/// <summary>
		/// Marks the token found.
		/// </summary>
		/// <returns><c>true</c>, if token id is newly found, <c>false</c> otherwise.</returns>
		/// <param name="id">Token identifier.</param>
		public bool MarkTokenFound (string id)
		{
			if (tokens.ContainsKey (id)) {
				if (!tokens [id].Found) {
					tokens [id].Found = true;
					if (TokenFound != null) {
						TokenFound (this, new TokenEventArgs { ID = id });
					}
					CheckAllTokensFound ();
					return true;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}

		public bool IsTokenFound (string id)
		{
			return tokens.ContainsKey (id) && tokens [id].Found;
		}

		async void CheckAllTokensFound ()
		{
			await Task.Run( (() => {
				lock (tokensLock) {
					foreach (Token token in tokens.Values) {
						if (!token.Found) {
							return;
						}
					}

					if (gameState == GameState.Start && AllTokensFound != null) {
						gameState = GameState.Over;
						if (GameStateChange != null) {
							GameStateChange (this, new GameStateEventArgs { GameState = gameState });
						}
						AllTokensFound (this, null);
					}
				}
			}));
		}
	}
}

