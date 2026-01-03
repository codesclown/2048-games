// Wait till the browser is ready to render the game (avoids glitches)
window.requestAnimationFrame(function () {
  // Initialize with Level 1 (4x4 grid) by default
  window.gameManager = new GameManager(4, KeyboardInputManager, HTMLActuator, LocalStorageManager);
  
  // Store current level
  window.currentLevel = 1;
  window.currentGridSize = 4;
  
  // Configure powerups for level 1 (4x4) - will be called from HTML when available
  if (typeof configurePowerUpsForLevel === 'function') {
    configurePowerUpsForLevel(4);
  }
  
  // Reset power-ups on initial game load
  setTimeout(() => {
    if (typeof resetPowerUps === 'function') {
      resetPowerUps();
    }
  }, 100);
  
});
