function GameManager(size, InputManager, Actuator, StorageManager) {
  this.size           = size; // Size of the grid
  this.inputManager   = new InputManager;
  this.storageManager = new StorageManager;
  this.actuator       = new Actuator;

  this.startTiles     = 2;

  this.inputManager.on("move", this.move.bind(this));
  this.inputManager.on("restart", this.restart.bind(this));
  this.inputManager.on("keepPlaying", this.keepPlaying.bind(this));

  this.setup();
}

// Restart the game
GameManager.prototype.restart = function () {
  console.log('🔄 RESTARTING GAME');
  this.storageManager.clearGameState();
  this.actuator.continueGame(); // Clear the game won/lost message
  this.setup();
};

// Force clear any corrupted state and restart
GameManager.prototype.forceRestart = function () {
  console.log('🚨 FORCE RESTART - Clearing all state');
  
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
  console.log('💥 COMPLETE RESET - Clearing everything');
  
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

  console.log('🔧 GAME SETUP:');
  console.log('  Grid size:', this.size);
  console.log('  Previous state:', previousState ? 'found' : 'none');

  // Always start fresh for new games - don't auto-load previous state
  console.log('  Creating fresh new game...');
  this.grid        = new Grid(this.size);
  this.score       = 0;
  this.over        = false;
  this.won         = false;
  this.keepPlaying = false;

  // Add the initial tiles
  this.addStartTiles();

  console.log('  Final setup state:');
  console.log('    this.over:', this.over);
  console.log('    this.won:', this.won);

  // Update the actuator
  this.actuate();
  
  // Debug initial state
  console.log('🎯 GAME SETUP COMPLETE:');
  this.logGridState();
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

  // Debug what we're sending to the UI
  console.log('🎭 ACTUATING UI STATE:');
  console.log('  this.over:', this.over);
  console.log('  this.won:', this.won);
  console.log('  this.isGameTerminated():', this.isGameTerminated());

  // Clear the state when the game is over (game over only, not win)
  if (this.over) {
    console.log('🗑️ Clearing game state because this.over = true');
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
  console.log('📋 SYNCHRONIZED GRID STATE (' + this.size + 'x' + this.size + '):');
  console.log('═══════════════════════════════════════');
  
  for (var y = 0; y < this.size; y++) {
    var row = [];
    var positions = [];
    for (var x = 0; x < this.size; x++) {
      var tile = this.grid.cellContent({ x: x, y: y });
      if (tile) {
        row.push(String(tile.value).padStart(4, ' '));
        positions.push('(' + x + ',' + y + ')=' + tile.value);
      } else {
        row.push('   .');
        positions.push('(' + x + ',' + y + ')=empty');
      }
    }
    console.log('  Row ' + y + ': [' + row.join('|') + ']');
    console.log('         ' + positions.join(', '));
  }
  
  // Count empty cells and show statistics
  var emptyCells = 0;
  var totalCells = this.size * this.size;
  for (var x = 0; x < this.size; x++) {
    for (var y = 0; y < this.size; y++) {
      if (!this.grid.cellContent({ x: x, y: y })) {
        emptyCells++;
      }
    }
  }
  console.log('═══════════════════════════════════════');
  console.log('  📊 Statistics:');
  console.log('    Empty cells: ' + emptyCells + '/' + totalCells);
  console.log('    Filled cells: ' + (totalCells - emptyCells) + '/' + totalCells);
  console.log('    Grid utilization: ' + Math.round(((totalCells - emptyCells) / totalCells) * 100) + '%');
};

// ROBUST GAME OVER DETECTION - Only real game over, no fake ones
GameManager.prototype.isRealGameOver = function () {
  console.log('🔍 COMPREHENSIVE GAME OVER CHECK:');
  console.log('  Grid size: ' + this.size + 'x' + this.size);
  
  // Step 1: Check for empty cells (if any empty, game is NOT over)
  var hasEmptyCells = this.grid.cellsAvailable();
  console.log('  ✓ Empty cells available: ' + hasEmptyCells);
  
  if (hasEmptyCells) {
    console.log('  ✅ GAME CONTINUES: Empty cells found');
    return false;
  }
  
  // Step 2: Check for possible merges (if any merge possible, game is NOT over)
  var hasPossibleMerges = this.tileMatchesAvailable();
  console.log('  ✓ Possible merges available: ' + hasPossibleMerges);
  
  if (hasPossibleMerges) {
    console.log('  ✅ GAME CONTINUES: Possible merges found');
    return false;
  }
  
  // Step 3: Double-check by testing actual moves
  var canMoveInAnyDirection = this.canMoveInAnyDirection();
  console.log('  ✓ Can move in any direction: ' + canMoveInAnyDirection);
  
  if (canMoveInAnyDirection) {
    console.log('  ✅ GAME CONTINUES: Movement possible');
    return false;
  }
  
  console.log('  🚨 REAL GAME OVER: No moves possible');
  this.logGridState();
  return true;
};

// Test if movement is possible in any direction
GameManager.prototype.canMoveInAnyDirection = function () {
  for (var direction = 0; direction < 4; direction++) {
    if (this.canMoveInDirection(direction)) {
      console.log('    ✓ Can move in direction ' + direction + ' (' + ['UP', 'RIGHT', 'DOWN', 'LEFT'][direction] + ')');
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

  console.log('🎮 MOVE ATTEMPT - Direction:', direction, '(' + ['UP', 'RIGHT', 'DOWN', 'LEFT'][direction] + ')');

  if (this.isGameTerminated()) {
    console.log('⏹️ Game already terminated, ignoring move');
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

          // The mighty 2048 tile
          if (merged.value === 2048) self.won = true;
        } else {
          self.moveTile(tile, positions.farthest);
        }

        if (!self.positionsEqual(cell, tile)) {
          moved = true; // The tile moved from its original cell!
        }
      }
    });
  });

  console.log('🔄 Move completed. Tiles moved:', moved);

  if (moved) {
    console.log('➕ Adding random tile...');
    this.addRandomTile();
    
    console.log('🔍 Checking if game is really over...');
    // Use robust game over detection instead of simple check
    if (this.isRealGameOver()) {
      console.log('🚨 CONFIRMED REAL GAME OVER!');
      this.over = true;
    } else {
      console.log('✅ Game continues - moves still available');
    }

    this.actuate();
  } else {
    console.log('❌ No tiles moved, move ignored');
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
  var cellsAvailable = this.grid.cellsAvailable();
  var matchesAvailable = this.tileMatchesAvailable();
  
  // Debug logging
  console.log('🔍 LEGACY MOVES AVAILABLE CHECK:');
  console.log('  Grid size:', this.size);
  console.log('  Cells available:', cellsAvailable);
  console.log('  Matches available:', matchesAvailable);
  console.log('  Total moves available:', cellsAvailable || matchesAvailable);
  
  return cellsAvailable || matchesAvailable;
};

// Check for available matches between tiles (more expensive check)
GameManager.prototype.tileMatchesAvailable = function () {
  var self = this;

  console.log('🔍 CHECKING TILE MATCHES:');
  
  for (var x = 0; x < this.size; x++) {
    for (var y = 0; y < this.size; y++) {
      var tile = this.grid.cellContent({ x: x, y: y });

      if (tile) {
        for (var direction = 0; direction < 4; direction++) {
          var vector = self.getVector(direction);
          var cell   = { x: x + vector.x, y: y + vector.y };

          // Check bounds BEFORE calling cellContent
          if (this.grid.withinBounds(cell)) {
            var other = self.grid.cellContent(cell);

            if (other && other.value === tile.value) {
              console.log('  ✅ Match found at (' + x + ',' + y + ') value=' + tile.value + ' with (' + cell.x + ',' + cell.y + ') value=' + other.value);
              return true; // These two tiles can be merged
            }
          }
        }
      }
    }
  }

  console.log('  ❌ No matches found');
  return false;
};

GameManager.prototype.positionsEqual = function (first, second) {
  return first.x === second.x && first.y === second.y;
};