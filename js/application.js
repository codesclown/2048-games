// Wait till the browser is ready to render the game (avoids glitches)
window.requestAnimationFrame(function () {
  // Initialize with Level 1 (4x4 grid) by default
  window.gameManager = new GameManager(4, KeyboardInputManager, HTMLActuator, LocalStorageManager);
  
  // Store current level
  window.currentLevel = 1;
  window.currentGridSize = 4;
  
  console.log('🎮 Game initialized with Level 1 (4x4 grid)');
});
