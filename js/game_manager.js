function GameManager(size, InputManager, Actuator, StorageManager) {
  this.size           = size; // Size of the grid
  this.inputManager   = new InputManager;
  this.storageManager = new StorageManager;
  this.actuator       = new Actuator;

  this.startTiles     = 2;
  
  // Dynamic winning score based on grid size/level
  // TEST MODE: Low values for quick testing
  if (this.size === 6) {
    this.winningValue = 8192; // Level 3
  } else if (this.size === 5) {
    this.winningValue = 4096; // Level 2
  } else {
    this.winningValue = 2048;  // Level 1
  }

  this.inputManager.on("move", this.move.bind(this));
  this.inputManager.on("restart", this.restart.bind(this));
  this.inputManager.on("keepPlaying", this.keepPlaying.bind(this));

  this.setup();
}



// Clean up input manager when destroying game manager
// This prevents double-input bugs when switching levels
GameManager.prototype.terminate = function() {
    if (this.inputManager && typeof this.inputManager.stop === 'function') {
        this.inputManager.stop();
    }
};

// Restart the game
GameManager.prototype.restart = function () {
  this.storageManager.clearGameState();
  this.actuator.continueGame(); // Clear the game won/lost message
  this.setup();
  
  // Reset reward video usage for new game
  if (typeof window !== 'undefined') {
    window.rewardVideoUsed = false;
  }
};

// Force clear any corrupted state and restart
GameManager.prototype.forceRestart = function () {
  // Clear all game state
  this.over = false;
  this.won = false;
  this.keepPlaying = false;
  this.score = 0;
  
  // Clear storage
  this.storageManager.clearGameState();
  
  // Clear UI
  this.actuator.continueGame();
  
  // Setup fresh game
  this.setup();
};

// Complete reset - clear everything including best score
GameManager.prototype.completeReset = function () {
  // Clear localStorage completely for this game
  if (typeof Storage !== "undefined") {
    localStorage.removeItem("gameState");
    localStorage.removeItem("bestScore");
  }
  
  this.forceRestart();
};

// Keep playing after winning (allows going over 2048)
GameManager.prototype.keepPlaying = function () {
  this.keepPlaying = true;
  this.actuator.continueGame(); // Clear the game won/lost message
};

// Return true if the game is lost, or has won and the user hasn't kept playing
GameManager.prototype.isGameTerminated = function () {
  return this.over || (this.won && !this.keepPlaying);
};

// Set up the game
GameManager.prototype.setup = function () {
  var previousState = this.storageManager.getGameState();

  // Always start fresh for new games - don't auto-load previous state
  this.grid        = new Grid(this.size);
  this.score       = 0;
  this.over        = false;
  this.won         = false;
  this.keepPlaying = false;

  // Add the initial tiles
  this.addStartTiles();

  // Update the actuator
  this.actuate();
};

// Set up the initial tiles to start the game with
GameManager.prototype.addStartTiles = function () {
  for (var i = 0; i < this.startTiles; i++) {
    this.addRandomTile();
  }
};

// Adds a tile in a random position
GameManager.prototype.addRandomTile = function () {
  if (this.grid.cellsAvailable()) {
    var value = Math.random() < 0.9 ? 2 : 4;
    var tile = new Tile(this.grid.randomAvailableCell(), value);

    this.grid.insertTile(tile);
  }
};

// Sends the updated grid to the actuator
GameManager.prototype.actuate = function () {
  if (this.storageManager.getBestScore() < this.score) {
    this.storageManager.setBestScore(this.score);
  }

  // Clear the state when the game is over (game over only, not win)
  if (this.over) {
    this.storageManager.clearGameState();
  } else {
    this.storageManager.setGameState(this.serialize());
  }

  this.actuator.actuate(this.grid, {
    score:      this.score,
    over:       this.over,
    won:        this.won,
    bestScore:  this.storageManager.getBestScore(),
    terminated: this.isGameTerminated()
  });

  // Auto-earn powerups based on tiles present
  if (typeof autoEarnPowerUps === 'function') {
    autoEarnPowerUps();
  }
};

// Represent the current game as an object
GameManager.prototype.serialize = function () {
  return {
    grid:        this.grid.serialize(),
    score:       this.score,
    over:        this.over,
    won:         this.won,
    keepPlaying: this.keepPlaying
  };
};

// Save all tile positions and remove merger info
GameManager.prototype.prepareTiles = function () {
  this.grid.eachCell(function (x, y, tile) {
    if (tile) {
      tile.mergedFrom = null;
      tile.savePosition();
    }
  });
};

// Move a tile and its representation
GameManager.prototype.moveTile = function (tile, cell) {
  this.grid.cells[tile.x][tile.y] = null;
  this.grid.cells[cell.x][cell.y] = tile;
  tile.updatePosition(cell);
};

// Enhanced debug method to show synchronized grid state
GameManager.prototype.logGridState = function () {
  
  for (var y = 0; y < this.size; y++) {
    var row = [];
    for (var x = 0; x < this.size; x++) {
      var tile = this.grid.cellContent({ x: x, y: y });
      if (tile) {
        row.push(String(tile.value).padStart(4, ' '));
      }
    }
    // console.log(row.join('|')); // formatting
    // console.log(row.join(' | '));
  }
};

// ROBUST GAME OVER DETECTION - Only real game over, no fake ones
GameManager.prototype.isRealGameOver = function () {
  // Step 1: Check for empty cells (if any empty, game is NOT over)
  // This is the fastest and most reliable check
  if (this.grid.cellsAvailable()) {
    return false;
  }
  
  // Step 2: Check for possible merges (horizontal and vertical)
  // This checks immediate neighbors for matching values
  if (this.tileMatchesAvailable()) {
    return false;
  }
  
  // Step 3: Double-check logic using the legacy method as a fallback
  // This ensures we don't accidentally end the game if one check fails
  if (this.movesAvailable()) {
     return false;
  }
  
  // If we get here, the grid is full AND there are no matches.
  // It really is Game Over.
  
  // console.log('!!! GAME OVER TRIGGERED !!!');
  // console.log('Grid Size:', this.size);
  // console.log('Empty Cells:', this.grid.availableCells().length);
  // Log visual representation
  this.logGridState();
  
  return true;
};

// Test if movement is possible in any direction
GameManager.prototype.canMoveInAnyDirection = function () {
  for (var direction = 0; direction < 4; direction++) {
    if (this.canMoveInDirection(direction)) {
      return true;
    }
  }
  return false;
};

// Test if movement is possible in a specific direction
GameManager.prototype.canMoveInDirection = function (direction) {
  var vector = this.getVector(direction);
  var traversals = this.buildTraversals(vector);
  var self = this;
  
  for (var i = 0; i < traversals.x.length; i++) {
    for (var j = 0; j < traversals.y.length; j++) {
      var x = traversals.x[i];
      var y = traversals.y[j];
      var cell = { x: x, y: y };
      var tile = this.grid.cellContent(cell);
      
      if (tile) {
        var positions = this.findFarthestPosition(cell, vector);
        var next = this.grid.cellContent(positions.next);
        
        // Can move if farthest position is different from current
        if (!this.positionsEqual(cell, positions.farthest)) {
          return true;
        }
        
        // Can move if can merge with next tile
        if (next && next.value === tile.value && !next.mergedFrom) {
          return true;
        }
      }
    }
  }
  
  return false;
};
// Move tiles on the grid in the specified direction
GameManager.prototype.move = function (direction) {
  // 0: up, 1: right, 2: down, 3: left
  var self = this;

  if (this.isGameTerminated()) {
    return; // Don't do anything if the game's over
  }

  var cell, tile;

  var vector     = this.getVector(direction);
  var traversals = this.buildTraversals(vector);
  var moved      = false;

  // Save the current tile positions and remove merger information
  this.prepareTiles();

  // Traverse the grid in the right direction and move tiles
  traversals.x.forEach(function (x) {
    traversals.y.forEach(function (y) {
      cell = { x: x, y: y };
      tile = self.grid.cellContent(cell);

      if (tile) {
        var positions = self.findFarthestPosition(cell, vector);
        var next      = self.grid.cellContent(positions.next);

        // Only one merger per row traversal?
        if (next && next.value === tile.value && !next.mergedFrom) {
          var merged = new Tile(positions.next, tile.value * 2);
          merged.mergedFrom = [tile, next];

          self.grid.insertTile(merged);
          self.grid.removeTile(tile);

          // Converge the two tiles' positions
          tile.updatePosition(positions.next);

          // Update the score
          self.score += merged.value;

          // Check for winning value
          if (merged.value === self.winningValue) self.won = true;
        } else {
          self.moveTile(tile, positions.farthest);
        }

        if (!self.positionsEqual(cell, tile)) {
          moved = true; // The tile moved from its original cell!
        }
      }
    });
  });

  if (moved) {
    this.addRandomTile();
    
    // Use robust game over detection instead of simple check
    if (this.isRealGameOver()) {
      this.over = true;
    }

    this.actuate();
  }
};

// Get the vector representing the chosen direction
GameManager.prototype.getVector = function (direction) {
  // Vectors representing tile movement
  var map = {
    0: { x: 0,  y: -1 }, // Up
    1: { x: 1,  y: 0 },  // Right
    2: { x: 0,  y: 1 },  // Down
    3: { x: -1, y: 0 }   // Left
  };

  return map[direction];
};

// Build a list of positions to traverse in the right order
GameManager.prototype.buildTraversals = function (vector) {
  var traversals = { x: [], y: [] };

  for (var pos = 0; pos < this.size; pos++) {
    traversals.x.push(pos);
    traversals.y.push(pos);
  }

  // Always traverse from the farthest cell in the chosen direction
  if (vector.x === 1) traversals.x = traversals.x.reverse();
  if (vector.y === 1) traversals.y = traversals.y.reverse();

  return traversals;
};

GameManager.prototype.findFarthestPosition = function (cell, vector) {
  var previous;

  // Progress towards the vector direction until an obstacle is found
  do {
    previous = cell;
    cell     = { x: previous.x + vector.x, y: previous.y + vector.y };
  } while (this.grid.withinBounds(cell) &&
           this.grid.cellAvailable(cell));

  return {
    farthest: previous,
    next: cell // Used to check if a merge is required
  };
};

// LEGACY COMPATIBILITY - Keep old method but use robust detection
GameManager.prototype.movesAvailable = function () {
  return this.grid.cellsAvailable() || this.tileMatchesAvailable();
};

// Check for available matches between tiles (more expensive check)
GameManager.prototype.tileMatchesAvailable = function () {
  var self = this;
  
  // console.log('Checking matches...');
  for (var x = 0; x < this.size; x++) {
    for (var y = 0; y < this.size; y++) {
      var tile = this.grid.cellContent({ x: x, y: y });

      if (tile) {
        for (var direction = 0; direction < 4; direction++) {
          var vector = self.getVector(direction);
          var cell   = { x: x + vector.x, y: y + vector.y };

          if (this.grid.withinBounds(cell)) {
            var other = self.grid.cellContent(cell);

            if (other && other.value === tile.value) {
              console.log('Match found!', x, y, 'with', cell.x, cell.y, 'val:', tile.value);
              return true; // These two tiles can be merged
            }
          }
        }
      }
    }
  }

  return false;
};

GameManager.prototype.positionsEqual = function (first, second) {
  return first.x === second.x && first.y === second.y;
};