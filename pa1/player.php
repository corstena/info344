<?php
/*
Albert Corsten INFO 344 PA 1
This class stores the stats related to a searched player as well the method for
displaying the player
*/
	class Player {

		private $name;
		private $team;
		private $gamesPlayed;
		private $PPG;
		private $FGmade;
		private $FGattempted;
		private $FGpercentage;
		private $threePtMade;
		private $threePtAttempted;
		private $threePtPercentage;
		private $FTmade;
		private $FTattempted;
		private $FTpercentage;
		private $reboundsOff;
		private $reboundsDef;
		private $assists;
		private $turnovers;
		private $steals;
		private $blocks;
		private $pFouls;

		function __construct($pName, $pTeam, $GP, $pointsPerGame, $fieldGoalMade, $fieldGoalAttempted, $fieldGoalPercentage, $threePointMade, $threePointAttempted, $threePointPercentage, $freeThrowMade, $freeThrowAttempted, $freeThrowPercentage, $rebOff, $rebDef, $ast, $TO, $stl, $blk, $fouls) {
			$this->name = $pName;
			$this->team = $pTeam;
			$this->gamesPlayed = $GP;
			$this->PPG = $pointsPerGame;
			$this->FGmade = $fieldGoalMade;
			$this->FGattempted = $fieldGoalAttempted;
			$this->FGpercentage = $fieldGoalPercentage;
			$this->threePtMade = $threePointMade;
			$this->threePtAttempted = $threePointAttempted;
			$this->threePtPercentage = $threePointPercentage;
			$this->FTmade = $freeThrowMade;
			$this->FTattempted = $freeThrowAttempted;
			$this->FTpercentage = $freeThrowPercentage;
			$this->reboundsOff = $rebOff;
			$this->reboundsDef = $rebDef;
			$this->assists = $ast;
			$this->turnovers = $TO;
			$this->steals = $stl;
			$this->blocks = $blk;
			$this->pFouls = $fouls;
		}

		public function displayPlayer() {
			echo("<div id=playerResult>
					<h2>$this->name</h2>
					<div id=pictureColumn class=column>
						<img src=profilePicture.jpg alt=Player Picture width=200px height=200px id=profilePicture>
					</div>

					<div id=playerStatsColumn class=column>
						<h3 class=statType>Player Stats</h3>
						<p>
							Team: $this->team <br>
							Games played: $this->gamesPlayed <br>
							Average PPG: $this->PPG <br>
							Average turnovers: $this->turnovers <br>
							Average steals: $this->steals <br>
						</p>
					</div>

					<div id=miscStatsColumn class=column>
						<h3 class=statType>Miscellaneous Stats</h3>
						<p>
							Average assists: $this->assists <br>
							Average blocks: $this->blocks <br>
							Average offensive rebounds: $this->reboundsOff <br>
							Average defensive rebounds: $this->reboundsDef <br>
							Average personal Fouls: $this->pFouls <br>
						</p>
					</div>

					<div id=gameStatsColumn class=column>
						<h3 class=statType>Game Stats</h3>
						<table>
							<tr>
								<td></td>
								<th>Made</th>
								<th>Attempted</th>
								<th>Percentage</th>
							</tr>
							<tr>
								<td>Field Goals</td>
								<td>$this->FGmade</td>
								<td>$this->FGattempted</td>
								<td>$this->FGpercentage</td>
							</tr>
							<tr>
								<td>3 Point Shots</td>
								<td>$this->threePtMade</td>
								<td>$this->threePtAttempted</td>
								<td>$this->threePtPercentage</td>
							</tr>
							<tr>
								<td>Free Throws</td>
								<td>$this->FTmade</td>
								<td>$this->FTattempted</td>
								<td>$this->FTpercentage</td>
							</tr>
						</table>
					</div>
				</div> ");
		}
}
?>
