using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
	//Number of rows and columns in the board, made flexible for testing, but probably should always both be 5
	public int rowNum;
	public int colNum;
	//Width and height of the board, should find a good value to set both to to fit the board on the screen
	public float boardWidth;
	public float boardHeight;
	//Reference for the script to be able to instanciate squares, and circles
	public GameObject squarePrefab;
	public GameObject circlePrefab;
	//a set of values for the z value of objects' transforms; included mostly for a lack of "magic numbers" in the code
	public float boardLayer;
	public float reticLayer;
	public float goalsLayer;
	public float goalsHLayer;
	public float playerLayer;
	//used to scale up and down squares adjustably and without magic numbers
	public float scalingFactor;
	//used to get reference to the camera
	public GameObject _camera;


	//Array containing references to all map pieces that still exist
	private GameObject[,] board;
	//rows and columns of the actual array of game objects which contains a perimiter buffer to keep the player in bounds
	private int arrRowNum;
	private int arrColNum;
	//to allow the script to reference the player, change its color, and keep track of its grid position
	private GameObject player;
	private SpriteRenderer playRend;
	private int plaRow;
	private int plaCol;
	//to allow the scrip to reference the reticle, change its color, and keep track of its grid position
	private GameObject reticle;
	private SpriteRenderer retRend;
	private int retRow;
	private int retCol;
	//Used to track who played first in current game, and whose turn it currently is. Under my convention, true means player 1 aka red player aka controls on the left (wasd+space) aka score on the left
	private bool startTurn;
	private bool currentTurn;
	//to tell if game is over
	private bool gameOver;
	//used to remember where to send player and retical back to on reset
	private Vector3 plaStartPos;
	private Vector3 retStartPos;

	// Use this for initialization
	void Start () {
		startTurn = true;
		currentTurn = true;
		gameOver = false;

		//Check for a suffciently large map to rule out edge cases
		if (rowNum < 3 || colNum < 3) {
			Debug.Log ("Board not big enough");
		}
		else {
			arrRowNum = rowNum + 2;
			arrColNum = colNum + 2;
			board = new GameObject[arrRowNum,arrColNum];
			for(int i=0; i<arrRowNum; i++){
				for(int j=0; j<arrColNum; j++){
					//check for to make sure you are NOT in buffer of the array around the actual board
					if (!((i == 0) || (j == 0) || (i == (arrRowNum - 1)) || (j == (arrColNum - 1)))) {
						Vector3 sDest = new Vector3 (j*(boardWidth/colNum),-1*i*(boardHeight/rowNum),boardLayer);
						board [i, j] = Instantiate (squarePrefab,sDest,Quaternion.identity);
						Vector3 sScale = new Vector3 (scalingFactor*boardWidth/colNum,scalingFactor*boardHeight/rowNum,1.0f);
						board [i, j].transform.localScale = sScale;

						//add goal-indicating circles as we go
						if(((i==1)&&(j==1))||((i==rowNum)&&(j==colNum))){
							Vector3 gDest = new Vector3 (j * (boardWidth / colNum), -1 * i * (boardHeight / rowNum), goalsLayer);
							Vector3 ghDest = new Vector3 (j * (boardWidth / colNum), -1 * i * (boardHeight / rowNum), goalsHLayer);
							GameObject currGoal = Instantiate (circlePrefab,gDest,Quaternion.identity);
							GameObject currGoalHole = Instantiate (circlePrefab,ghDest,Quaternion.identity);
							Vector3 gScale = sScale;
							currGoal.transform.localScale = gScale*0.95f;
							currGoalHole.transform.localScale = gScale*0.75f;
							SpriteRenderer gRenderer = currGoal.GetComponent<SpriteRenderer> ();
							SpriteRenderer ghRenderer = currGoalHole.GetComponent<SpriteRenderer> ();
							gRenderer.color = Color.red;
							ghRenderer.color = Color.white;
						}
						if(((i==rowNum)&&(j==1))||((i==1)&&(j==colNum))){
							Vector3 gDest = new Vector3 (j * (boardWidth / colNum), -1 * i * (boardHeight / rowNum), goalsLayer);
							Vector3 ghDest = new Vector3 (j * (boardWidth / colNum), -1 * i * (boardHeight / rowNum), goalsHLayer);
							GameObject currGoal = Instantiate (circlePrefab,gDest,Quaternion.identity);
							GameObject currGoalHole = Instantiate (circlePrefab,ghDest,Quaternion.identity);
							Vector3 gScale = sScale;
							currGoal.transform.localScale = gScale*0.95f;
							currGoalHole.transform.localScale = gScale*0.75f;
							SpriteRenderer gRenderer = currGoal.GetComponent<SpriteRenderer> ();
							SpriteRenderer ghRenderer = currGoalHole.GetComponent<SpriteRenderer> ();
							gRenderer.color = Color.yellow;
							ghRenderer.color = Color.white;
						}
						//add player token and reticle as we go
						if((i==((rowNum/2)+1))&&(j==((colNum/2)+1))){
							Vector3 pDest = new Vector3 (j * (boardWidth / colNum), -1 * i * (boardHeight / rowNum), playerLayer);
							Vector3 rDest = new Vector3 (j * (boardWidth / colNum), -1 * i * (boardHeight / rowNum), reticLayer);
							GameObject currPlayer = Instantiate (circlePrefab,pDest,Quaternion.identity);
							GameObject currReticl = Instantiate (squarePrefab,rDest,Quaternion.identity);
							Vector3 pScale = sScale;
							currPlayer.transform.localScale = pScale*0.75f;
							currReticl.transform.localScale = pScale;
							SpriteRenderer pRenderer = currPlayer.GetComponent<SpriteRenderer> ();
							SpriteRenderer rRenderer = currReticl.GetComponent<SpriteRenderer> ();
							pRenderer.color = Color.red;
							rRenderer.color = Color.green;

							player = currPlayer;
							reticle = currReticl;
							plaRow = i;
							plaCol = j;
							retRow = i;
							retCol = j;
							playRend = pRenderer;
							retRend = rRenderer;
							plaStartPos = pDest;
							retStartPos = rDest;
						}
					}
					//what to do if you are in the boarder
					else {
						board [i,j] = null;
					}
				}
			}
			//set camera to middle of board 
			Vector3 cDest = new Vector3(((arrColNum-1)*(boardWidth/colNum))/2.0f,-1*(((arrRowNum-1)*(boardHeight/rowNum))/2.0f),-10.0f);
			_camera.transform.position = cDest;
		}
	}
	
	// Update is called once per frame
	void Update () {
		//only listen for player controls if game is not over
		if(!gameOver){
			//Only do player one controls when it is player one's turn
			if (currentTurn) {
				//Move reticle
				if (Input.GetKeyDown ("w") && plaRow - retRow < 1 && retRow > 1) {
					reticle.transform.Translate (0.0f, boardHeight / rowNum, 0.0f);
					retRow--;
				}
				if (Input.GetKeyDown ("s") && plaRow - retRow > -1 && retRow < rowNum) {
					reticle.transform.Translate (0.0f, -1 * boardHeight / rowNum, 0.0f);
					retRow++;
				}
				if (Input.GetKeyDown ("a") && plaCol - retCol < 1 && retCol > 1) {
					reticle.transform.Translate (-1 * boardWidth / colNum, 0.0f, 0.0f);
					retCol--;
				}
				if (Input.GetKeyDown ("d") && plaCol - retCol > -1 && retCol < colNum) {
					reticle.transform.Translate (boardWidth / colNum, 0.0f, 0.0f);
					retCol++;
				}
				//Move player to reticle
				if (Input.GetKeyDown ("space") && (retRow != plaRow || retCol != plaCol) && board [retRow, retCol] != null) {
					Destroy (board [plaRow, plaCol]);
					Vector3 mDest = new Vector3 ();
					mDest.x = reticle.transform.position.x;
					mDest.y = reticle.transform.position.y;
					mDest.z = playerLayer;
					player.transform.position = mDest;
					plaRow = retRow;
					plaCol = retCol;
					currentTurn = !currentTurn;
					flipColor ();
					checkWin ();
					checkDraw ();
				}
			}
			//Only do player two controls when it is player two's turn
			else {
				//Move reticle
				if (Input.GetKeyDown ("up") && plaRow - retRow < 1 && retRow > 1) {
					reticle.transform.Translate (0.0f, boardHeight / rowNum, 0.0f);
					retRow--;
				}
				if (Input.GetKeyDown ("down") && plaRow - retRow > -1 && retRow < rowNum) {
					reticle.transform.Translate (0.0f, -1 * boardHeight / rowNum, 0.0f);
					retRow++;
				}
				if (Input.GetKeyDown ("left") && plaCol - retCol < 1 && retCol > 1) {
					reticle.transform.Translate (-1 * boardWidth / colNum, 0.0f, 0.0f);
					retCol--;
				}
				if (Input.GetKeyDown ("right") && plaCol - retCol > -1 && retCol < colNum) {
					reticle.transform.Translate (boardWidth / colNum, 0.0f, 0.0f);
					retCol++;
				}
				//Move player to reticle
				if (Input.GetKeyDown ("return") && (retRow != plaRow || retCol != plaCol) && board [retRow, retCol] != null) {
					Destroy (board [plaRow, plaCol]);
					Vector3 mDest = new Vector3 ();
					mDest.x = reticle.transform.position.x;
					mDest.y = reticle.transform.position.y;
					mDest.z = playerLayer;
					player.transform.position = mDest;
					plaRow = retRow;
					plaCol = retCol;
					currentTurn = !currentTurn;
					flipColor ();
					checkWin ();
					checkDraw ();
				}
			}
		}
		//if game is over, press space or enter to restart
		else{
			if(Input.GetKeyDown("space")||Input.GetKeyDown("return")){
				reset ();
			}
		}
		if(Input.GetKeyDown("r")){
			reset ();
		}
	}

	private void flipColor(){
		if(currentTurn){
			playRend.color = Color.red;
		}
		else{
			playRend.color = Color.yellow;
		}
	}

	private void checkWin(){
		if(((plaRow==1)&&(plaCol==1))||((plaRow==rowNum)&&(plaCol==colNum))){
			playRend.color = Color.red;
			gameOver = true;
		}
		if(((plaRow==rowNum)&&(plaCol==1))||((plaRow==1)&&(plaCol==colNum))){
			playRend.color = Color.yellow;
			gameOver = true;
		}
	}

	private void checkDraw(){
		/*
		if(board[plaRow-1,plaCol-1]==null&&board[plaRow-1,plaCol+1]==null&&board[plaRow+1,plaCol-1]==null&&board[plaRow+1,plaCol+1]==null&&board[plaRow-1,plaCol]==null&&board[plaRow+1,plaCol]==null&&board[plaRow,plaCol-1]==null&&board[plaRow,plaCol+1]==null){
			gameOver=true;
		}
		*/
	}

	private void reset(){
		for(int i=1; i<=rowNum; i++){
			for(int j = 1; j<=colNum; j++){
				//fill holes in board
				if(board[i,j]==null){
					Vector3 sDest = new Vector3 (j*(boardWidth/colNum),-1*i*(boardHeight/rowNum),boardLayer);
					board [i, j] = Instantiate (squarePrefab,sDest,Quaternion.identity);
					Vector3 sScale = new Vector3 (scalingFactor*boardWidth/colNum,scalingFactor*boardHeight/rowNum,1.0f);
					board [i, j].transform.localScale = sScale;
				}
				//restore position of player and reticle as well as the stored values to keep track of them
				player.transform.position = plaStartPos;
				reticle.transform.position = retStartPos;
				plaRow = (rowNum / 2) + 1;
				plaCol = (colNum / 2) + 1;
				retRow = (rowNum / 2) + 1;
				retCol = (colNum / 2) + 1;
				//handle player turn and make game not over
				startTurn = !startTurn;
				currentTurn = startTurn;
				gameOver = false;
				if(currentTurn){
					playRend.color = Color.red;
				}
				else{
					playRend.color = Color.yellow;
				}
			}
		}
	}
}
