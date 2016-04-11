<?php
/*
Albert Corsten INFO 344 PA 1
This file contains fucntions common to several pages of the NBA player search website
*/
function page_top() { ?>
<!DOCTYPE html>
<html>
	<head>
		<title>NBA Player Statistics Search</title>
		<meta charset="utf-8" />
		<link rel="stylesheet" type="text/css" href="nba.css">
	</head>

	<body>
		<div id="logo">
			<h1> NBA Player Statistics Search <img src="logo.png" alt="NBA Logo" id="logo"> </h1>
		</div>
		<form method="get" action="search.php">
			<div id="search">
				<input id="searchBox" type="text" name="playerSearch" size="40" placeholder="Enter player name here"/>
				<input type="submit" value="Search">
			</div>
		</form>

	</body>
</html>
<?php }

function footer() {
echo("<div id=footer>Albert Corsten | INFO 344 | Programming Assignment 1</div>");
}
?>