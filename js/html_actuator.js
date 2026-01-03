function HTMLActuator() {
  this.tileContainer    = document.querySelector(".tile-container");
  this.scoreContainer   = document.querySelector(".score-container");
  this.bestContainer    = document.querySelector(".best-container");
  this.messageContainer = document.querySelector(".game-message");

  this.score = 0;
  
  // If messageContainer doesn't exist, we'll handle it gracefully
  if (!this.messageContainer) {
    console.log('Game message container not found - will use ultimate button fix for game over');
  }
}

HTMLActuator.prototype.actuate = function (grid, metadata) {
  var self = this;

  window.requestAnimationFrame(function () {
    self.clearContainer(self.tileContainer);

    grid.cells.forEach(function (column) {
      column.forEach(function (cell) {
        if (cell) {
          self.addTile(cell);
        }
      });
    });

    self.updateScore(metadata.score);
    self.updateBestScore(metadata.bestScore);

    if (metadata.terminated) {
      if (metadata.over) {
        self.message(false); // You lose
      } else if (metadata.won) {
        self.message(true); // You win!
      }
    } else {
      self.clearMessage();
    }

  });
};

// Continues the game (both restart and keep playing)
HTMLActuator.prototype.continueGame = function () {
  this.clearMessage();
};

HTMLActuator.prototype.clearContainer = function (container) {
  while (container.firstChild) {
    container.removeChild(container.firstChild);
  }
};

HTMLActuator.prototype.addTile = function (tile) {
  var self = this;

  var wrapper   = document.createElement("div");
  var inner     = document.createElement("div");
  var position  = tile.previousPosition || { x: tile.x, y: tile.y };
  var positionClass = this.positionClass(position);

  // We can't use classlist because it somehow glitches when replacing classes
  var classes = ["tile", "tile-" + tile.value, positionClass];

  if (tile.value > 2048) classes.push("tile-super");

  this.applyClasses(wrapper, classes);

  inner.classList.add("tile-inner");
  inner.textContent = tile.value;

  if (tile.previousPosition) {
    // Make sure that the tile gets rendered in the previous position first
    window.requestAnimationFrame(function () {
      classes[2] = self.positionClass({ x: tile.x, y: tile.y });
      self.applyClasses(wrapper, classes); // Update the position
      
      // Play slide sound for tile movement
      if (window.gameSounds) {
        window.gameSounds.playSlide();
      }
    });
  } else if (tile.mergedFrom) {
    classes.push("tile-merged");
    this.applyClasses(wrapper, classes);

    // Play merge sound
    if (window.gameSounds) {
      window.gameSounds.playMerge(tile.value);
    }

    // Render the tiles that merged
    tile.mergedFrom.forEach(function (merged) {
      self.addTile(merged);
    });
  } else {
    classes.push("tile-new");
    this.applyClasses(wrapper, classes);
    
    // Play spawn sound for new tiles
    if (window.gameSounds) {
      window.gameSounds.playSpawn();
    }
  }

  // Add the inner part of the tile to the wrapper
  wrapper.appendChild(inner);

  // Put the tile on the board
  this.tileContainer.appendChild(wrapper);
};

HTMLActuator.prototype.applyClasses = function (element, classes) {
  element.setAttribute("class", classes.join(" "));
};

HTMLActuator.prototype.normalizePosition = function (position) {
  return { x: position.x + 1, y: position.y + 1 };
};

HTMLActuator.prototype.positionClass = function (position) {
  position = this.normalizePosition(position);
  return "tile-position-" + position.x + "-" + position.y;
};

HTMLActuator.prototype.updateScore = function (score) {
  this.clearContainer(this.scoreContainer);

  var difference = score - this.score;
  this.score = score;

  // Create a span for the score number to preserve the CSS :after label
  var scoreNumber = document.createElement("span");
  scoreNumber.classList.add("score-number");
  scoreNumber.textContent = this.score;
  this.scoreContainer.appendChild(scoreNumber);

  if (difference > 0) {
    var addition = document.createElement("div");
    addition.classList.add("score-addition");
    addition.textContent = "+" + difference;

    this.scoreContainer.appendChild(addition);
  }
};

HTMLActuator.prototype.updateBestScore = function (bestScore) {
  // Clear and create a span for the best score number to preserve the CSS :after label
  this.bestContainer.innerHTML = '';
  var bestNumber = document.createElement("span");
  bestNumber.classList.add("score-number");
  bestNumber.textContent = bestScore;
  this.bestContainer.appendChild(bestNumber);
};

HTMLActuator.prototype.message = function (won) {
  var type    = won ? "game-won" : "game-over";
  var message = won ? "You win!" : "Game over!";

  // Check if messageContainer exists before using it
  if (this.messageContainer) {
    this.messageContainer.classList.add(type);
    var messageP = this.messageContainer.getElementsByTagName("p")[0];
    if (messageP) {
      messageP.textContent = message;
    }
  }
  
  // Play win/loss sounds
  if (window.gameSounds) {
    if (won) {
      window.gameSounds.playWin();
    } else {
      window.gameSounds.playLoss();
    }
  }
  
  // Show the full-screen modal for our custom game over screen
  if (!won) {
    var self = this;
    setTimeout(function() {
      // Use ultimate button fix directly since original elements might not exist
      if (typeof window.ultimateButtonFix === 'function') {
        window.ultimateButtonFix();
      }
    }, 50); // Faster game over screen appearance
  }
};

HTMLActuator.prototype.clearMessage = function () {
  // IE only takes one value to remove at a time.
  if (this.messageContainer) {
    this.messageContainer.classList.remove("game-won");
    this.messageContainer.classList.remove("game-over");
    this.messageContainer.classList.remove("show-modal");
    
    // Also reset the display style for our custom full-screen modal
    this.messageContainer.style.display = '';
  }
};
