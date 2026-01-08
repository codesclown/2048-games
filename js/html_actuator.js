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
  
  // Wire up buttons dynamically since ui_logic.js is missing
  // This ensures "Try Again" and "Home" work even after revert
  var self = this;
  setTimeout(function() {
      // Retry Button
      var retryBtn = document.getElementById('retryBtn');
      if (retryBtn) {
          retryBtn.onclick = function(e) {
              e.preventDefault();
              
              // Post score and show ad on retry
              if (typeof postScore === 'function') {
                  postScore(self.score);
              }
              if (typeof showAd === 'function') {
                  showAd();
              }
              
              if (window.gameManager) {
                  window.gameManager.restart();
              } else {
                  // Fallback if gameManager missing
                  location.reload(); 
              }
          };
      }
      
      // Home Button - Redirects to home/reload since we lost the home screen logic
      var homeBtn = document.getElementById('homeBtn');
      if (homeBtn) {
          homeBtn.onclick = function(e) {
             e.preventDefault();
             
             // Post score and show ad on home
             if (typeof postScore === 'function') {
                  postScore(self.score);
             }
             if (typeof showAd === 'function') {
                  showAd();
             }

             location.reload();
          };
      }

      // Keep Playing Button
      var keepPlayingBtn = document.getElementById('keepPlayingBtn');
      if (keepPlayingBtn) {
          keepPlayingBtn.onclick = function(e) {
              e.preventDefault();
              if (window.gameManager) {
                  window.gameManager.keepPlaying();
              }
          };
      }
  }, 1000);
}

HTMLActuator.prototype.actuate = function (grid, metadata) {
  var self = this;

  // CLONE the grid cells synchronously to capture the state at this exact moment.
  // This prevents race conditions where logic updates the grid before the next animation frame.
  var cellsSnapshot = [];
  for (var x = 0; x < grid.size; x++) {
    var row = [];
    for (var y = 0; y < grid.size; y++) {
      var tile = grid.cells[x][y];
      if (tile) {
        // We need to clone the tile object too, especially previousPosition and mergedFrom
        var tileClone = {
          x: tile.x,
          y: tile.y,
          value: tile.value,
          previousPosition: tile.previousPosition ? { x: tile.previousPosition.x, y: tile.previousPosition.y } : null,
          mergedFrom: null
        };
        
        if (tile.mergedFrom) {
          tileClone.mergedFrom = tile.mergedFrom.map(function(t) {
             return {
               x: t.x,
               y: t.y,
               value: t.value,
               previousPosition: t.previousPosition
             };
          });
        }
        row.push(tileClone);
      } else {
        row.push(null);
      }
    }
    cellsSnapshot.push(row);
  }

  window.requestAnimationFrame(function () {
    self.clearContainer(self.tileContainer);

    // Use the snapshot!
    cellsSnapshot.forEach(function (column) {
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

  // Self-healing: Inject CSS for larger grids if missing
  // This fixes the issue where 5x5/6x6 tiles are invisible or stacked because ui_logic.js is missing
  if (grid.size > 4 && this.currentGridSize !== grid.size) {
    this.injectGridCSS(grid.size);
    this.currentGridSize = grid.size;
  }
};

// DYNAMIC CSS INJECTION to support 5x5 and 6x6 grids without external dependencies
HTMLActuator.prototype.injectGridCSS = function(size) {
    const styleId = 'dynamic-grid-style';
    let styleEl = document.getElementById(styleId);
    
    if (!styleEl) {
        styleEl = document.createElement('style');
        styleEl.id = styleId;
        document.head.appendChild(styleEl);
    }
    
    // Default main.css is optimal for 4x4. We only override for valid larger sizes.
    if (size <= 4) {
        styleEl.textContent = '';
        return;
    }

    // Responsive logic using calc() and CSS vars to fit perfectly in the existing container
    // We strictly use the parent container's padding to ensure alignment
    const css = `
        :root {
            --grid-gap: 15px;
            --grid-padding: 15px;
            --grid-size: ${size};
        }
        @media screen and (max-width: 520px) {
            :root {
                --grid-gap: 10px;
                --grid-padding: 10px;
            }
        }
        
        /* Constrain grid container to the CONTENT box of the parent */
        /* Parent .game-container has padding, so we must subtract it */
        .grid-container, .tile-container {
            width: calc(100% - 2 * var(--grid-padding));
            height: calc(100% - 2 * var(--grid-padding));
            left: var(--grid-padding);
            top: var(--grid-padding);
            right: auto;
            bottom: auto;
            margin: 0; 
            position: absolute;
        }

        .grid-cell {
            width: calc((100% - (var(--grid-size) - 1) * var(--grid-gap)) / var(--grid-size));
            height: calc((100% - (var(--grid-size) - 1) * var(--grid-gap)) / var(--grid-size));
            margin-right: var(--grid-gap);
            margin-bottom: var(--grid-gap);
            float: left;
        }
        
        /* Remove margins from last items in each row to fit perfectly */
        .grid-cell:nth-of-type(${size}n) {
            margin-right: 0;
        }
        /* Remove bottom margin from last row */
        .grid-cell:nth-of-type(n + ${size * (size - 1) + 1}) {
            margin-bottom: 0;
        }
        
        .tile, .tile .tile-inner {
            width: calc((100% - (var(--grid-size) - 1) * var(--grid-gap)) / var(--grid-size));
            height: calc((100% - (var(--grid-size) - 1) * var(--grid-gap)) / var(--grid-size));
            line-height: calc((100% - (var(--grid-size) - 1) * var(--grid-gap)) / var(--grid-size));
        }
        
        /* Generate positions for dynamic size */
        ${this.generatePositionCSS(size)}
        
        /* Font size adjustments */
        .tile .tile-inner {
            font-size: ${size >= 6 ? '20px' : size === 5 ? '30px' : '35px'};
        }
        @media screen and (max-width: 520px) {
            .tile .tile-inner {
                font-size: ${size >= 6 ? '10px' : size === 5 ? '15px' : '20px'};
            }
        }
    `;
    
    styleEl.textContent = css;
};

HTMLActuator.prototype.generatePositionCSS = function(size) {
    let css = '';
    for (let x = 0; x < size; x++) {
        for (let y = 0; y < size; y++) {
            // Formula in CSS: index * (cellWidth + gap)
            // cellWidth + gap = (100% - (size-1)*gap)/size + gap
            const positionFormula = `(100% - (${size} - 1) * var(--grid-gap)) / ${size} + var(--grid-gap)`;
            
            const xFormula = `calc(${x} * (${positionFormula}))`;
            const yFormula = `calc(${y} * (${positionFormula}))`;
            
            css += `
                .tile.tile-position-${x + 1}-${y + 1} {
                    -webkit-transform: translate(${xFormula}, ${yFormula});
                    -moz-transform: translate(${xFormula}, ${yFormula});
                    transform: translate(${xFormula}, ${yFormula});
                }
            `;
        }
    }
    return css;
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
    // Hide Keep Playing button for Game Over
    var keepPlayingBtn = document.getElementById('keepPlayingBtn');
    if (keepPlayingBtn) keepPlayingBtn.style.display = 'none';

    // Show reward video popup first before game over
    this.showRewardVideoPopup();
  } else {
    // Show Keep Playing button for Win
    var keepPlayingBtn = document.getElementById('keepPlayingBtn');
    if (keepPlayingBtn) keepPlayingBtn.style.display = 'inline-block';

    // Use the robust ultimate fix to show the win popup
    // This ensures consistency with how game over is shown
    if (typeof window.ultimateButtonFix === 'function') {
        window.ultimateButtonFix(true);
    }
  }
};

// New method to show reward video popup
HTMLActuator.prototype.showRewardVideoPopup = function() {
  var self = this;
  
  // Check if reward video has already been used in this session
  if (window.rewardVideoUsed) {
    console.log('Reward video already used this session, showing game over directly');
    // Show game over directly
    setTimeout(function() {
      if (typeof window.ultimateButtonFix === 'function') {
        window.ultimateButtonFix();
      }
    }, 50);
    return;
  }
  
  // Show reward video popup for first time
  if (typeof window.showRewardVideoPopup === 'function') {
    console.log('First game over - showing reward video popup');
    window.showRewardVideoPopup();
  } else {
    // Fallback to direct game over if popup function doesn't exist
    setTimeout(function() {
      if (typeof window.ultimateButtonFix === 'function') {
        window.ultimateButtonFix();
      }
    }, 50);
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
