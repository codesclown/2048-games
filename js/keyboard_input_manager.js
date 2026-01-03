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

  // Respond to direction keys
  document.addEventListener("keydown", function (event) {
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
  });

  // Respond to button presses
  // Note: .retry-button and .mobile-refresh-btn use custom onclick handlers that reset power-ups
  this.bindButtonPress(".keep-playing-button", this.keepPlaying);

  // Respond to swipe events
  var touchStartClientX, touchStartClientY;
  var touchStartTime;
  var gameContainer = document.getElementsByClassName("game-container")[0];

  gameContainer.addEventListener(this.eventTouchstart, function (event) {
    if ((!window.navigator.msPointerEnabled && event.touches.length > 1) ||
        event.targetTouches.length > 1) {
      return; // Ignore if touching with more than 1 finger
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
  });

  gameContainer.addEventListener(this.eventTouchmove, function (event) {
    // Ultra-fast swipe detection during move for instant response
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
      
      // FASTER: Reduced threshold from 8 to 5 for instant response
      if (distance > 5) { // Even faster detection
        var direction = absDx > absDy ? (dx > 0 ? 1 : 3) : (dy > 0 ? 2 : 0);
        
        // Clear touch coordinates to prevent multiple triggers
        touchStartClientX = undefined;
        touchStartClientY = undefined;
        
        // Emit move immediately for ultra-fast response
        self.emit("move", direction);
      }
    }
    
    event.preventDefault();
  });

  gameContainer.addEventListener(this.eventTouchend, function (event) {
    if ((!window.navigator.msPointerEnabled && event.touches.length > 0) ||
        event.targetTouches.length > 0) {
      return; // Ignore if still touching with one or more fingers
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

    // FASTER: Ultra-sensitive swipe detection for fastest possible response
    // Even lower thresholds for instant detection
    var threshold = velocity > 0.8 ? 2 : (velocity > 0.4 ? 3 : 5); // Super fast swipes need only 2px

    if (distance > threshold) {
      // Immediate direction detection for faster response
      var direction = absDx > absDy ? (dx > 0 ? 1 : 3) : (dy > 0 ? 2 : 0);
      
      // Emit move immediately without delay
      self.emit("move", direction);
    }
  });
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
