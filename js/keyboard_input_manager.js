function KeyboardInputManager() {
  this.events = {};

  if (window.navigator.msPointerEnabled) {
    //Internet Explorer 10 style
    this.eventTouchstart    = "MSPointerDown";
    this.eventTouchmove     = "MSPointerMove";
    this.eventTouchend      = "MSPointerUp";
  } else {
    this.eventTouchstart    = "touchstart";
    this.eventTouchmove     = "touchmove";
    this.eventTouchend      = "touchend";
  }

  this.listen();
}



KeyboardInputManager.prototype.on = function (event, callback) {
  if (!this.events[event]) {
    this.events[event] = [];
  }
  this.events[event].push(callback);
};

KeyboardInputManager.prototype.emit = function (event, data) {
  var callbacks = this.events[event];
  if (callbacks) {
    callbacks.forEach(function (callback) {
      callback(data);
    });
  }
};

KeyboardInputManager.prototype.listen = function () {
  var self = this;

  var map = {
    38: 0, // Up
    39: 1, // Right
    40: 2, // Down
    37: 3, // Left
    75: 0, // Vim up
    76: 1, // Vim right
    74: 2, // Vim down
    72: 3, // Vim left
    87: 0, // W
    68: 1, // D
    83: 2, // S
    65: 3  // A
  };

  // Cached handler for keydown to allow removal
  this.handleKeydown = function (event) {
    var modifiers = event.altKey || event.ctrlKey || event.metaKey ||
                    event.shiftKey;
    var mapped    = map[event.which];

    if (!modifiers) {
      if (mapped !== undefined) {
        event.preventDefault();
        self.emit("move", mapped);
      }
    }

    // R key restarts the game
    if (!modifiers && event.which === 82) {
      self.restart.call(self, event);
    }
  };

  // Respond to direction keys
  document.addEventListener("keydown", this.handleKeydown);

  // Respond to button presses
  this.bindButtonPress(".keep-playing-button", this.keepPlaying);

  // Respond to swipe events
  var touchStartClientX, touchStartClientY;
  var touchStartTime;
  var gameContainer = document.querySelector(".game-container");
  
  // Guard against missing container
  if (!gameContainer) return;

  this.handleTouchStart = function (event) {
    if ((!window.navigator.msPointerEnabled && event.touches.length > 1) ||
        event.targetTouches.length > 1) {
      return;
    }

    if (window.navigator.msPointerEnabled) {
      touchStartClientX = event.pageX;
      touchStartClientY = event.pageY;
    } else {
      touchStartClientX = event.touches[0].clientX;
      touchStartClientY = event.touches[0].clientY;
    }

    touchStartTime = Date.now();
    event.preventDefault();
  };

  this.handleTouchMove = function (event) {
    if (touchStartClientX !== undefined && touchStartClientY !== undefined) {
      var currentX, currentY;
      
      if (window.navigator.msPointerEnabled) {
        currentX = event.pageX;
        currentY = event.pageY;
      } else {
        currentX = event.touches[0].clientX;
        currentY = event.touches[0].clientY;
      }

      var dx = currentX - touchStartClientX;
      var absDx = Math.abs(dx);
      var dy = currentY - touchStartClientY;
      var absDy = Math.abs(dy);
      
      var distance = Math.max(absDx, absDy);
      
      if (distance > 5) {
        var direction = absDx > absDy ? (dx > 0 ? 1 : 3) : (dy > 0 ? 2 : 0);
        touchStartClientX = undefined;
        touchStartClientY = undefined;
        self.emit("move", direction);
      }
    }
    event.preventDefault();
  };

  this.handleTouchEnd = function (event) {
    if ((!window.navigator.msPointerEnabled && event.touches.length > 0) ||
        event.targetTouches.length > 0) {
      return;
    }

    var touchEndClientX, touchEndClientY;

    if (window.navigator.msPointerEnabled) {
      touchEndClientX = event.pageX;
      touchEndClientY = event.pageY;
    } else {
      touchEndClientX = event.changedTouches[0].clientX;
      touchEndClientY = event.changedTouches[0].clientY;
    }

    var dx = touchEndClientX - touchStartClientX;
    var absDx = Math.abs(dx);
    var dy = touchEndClientY - touchStartClientY;
    var absDy = Math.abs(dy);
    var touchDuration = Date.now() - touchStartTime;
    var distance = Math.max(absDx, absDy);
    var velocity = distance / touchDuration;
    var threshold = velocity > 0.8 ? 2 : (velocity > 0.4 ? 3 : 5);

    if (distance > threshold) {
      var direction = absDx > absDy ? (dx > 0 ? 1 : 3) : (dy > 0 ? 2 : 0);
      self.emit("move", direction);
    }
  };

  gameContainer.addEventListener(this.eventTouchstart, this.handleTouchStart, { passive: false });
  gameContainer.addEventListener(this.eventTouchmove, this.handleTouchMove, { passive: false });
  gameContainer.addEventListener(this.eventTouchend, this.handleTouchEnd, { passive: false });
};

// Cleanup method to remove event listeners
KeyboardInputManager.prototype.stop = function () {
  document.removeEventListener("keydown", this.handleKeydown);
  
  var gameContainer = document.querySelector(".game-container");
  if (gameContainer) {
    if (this.handleTouchStart) gameContainer.removeEventListener(this.eventTouchstart, this.handleTouchStart);
    if (this.handleTouchMove) gameContainer.removeEventListener(this.eventTouchmove, this.handleTouchMove);
    if (this.handleTouchEnd) gameContainer.removeEventListener(this.eventTouchend, this.handleTouchEnd);
  }
};

KeyboardInputManager.prototype.restart = function (event) {
  event.preventDefault();
  this.emit("restart");
};

KeyboardInputManager.prototype.keepPlaying = function (event) {
  event.preventDefault();
  this.emit("keepPlaying");
};

KeyboardInputManager.prototype.bindButtonPress = function (selector, fn) {
  var button = document.querySelector(selector);
  if (button) {
    button.addEventListener("click", fn.bind(this));
    button.addEventListener(this.eventTouchend, fn.bind(this));
  }
};
