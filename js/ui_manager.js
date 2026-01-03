/**
 * UI Manager - Production-ready UI management with bulletproof event handling
 * Fixes all button click issues and popup problems
 */

class UIManager {
    constructor() {
        this.eventListeners = new Map();
        this.modalStack = [];
        this.isInitialized = false;
        this.buttonClickHandlers = new Map();
        
        this.initialize();
    }
    
    initialize() {
        if (this.isInitialized) return;
        
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.setupUI());
        } else {
            this.setupUI();
        }
        
        this.isInitialized = true;
    }
    
    setupUI() {
        this.setupGameOverModal();
        this.setupButtonHandlers();
        this.setupGlobalEventDelegation();
        this.setupStateBasedUI();
        
        console.log('UI Manager initialized');
    }
    
    // Setup bulletproof game over modal
    setupGameOverModal() {
        const gameMessage = document.querySelector('.game-message');
        if (!gameMessage) {
            console.error('Game message container not found');
            return;
        }
        
        // Ensure proper CSS for modal
        this.ensureModalCSS();
        
        // Setup modal event handling
        this.setupModalEventHandling(gameMessage);
    }
    
    ensureModalCSS() {
        const style = document.createElement('style');
        style.textContent = `
            /* Bulletproof Game Over Modal */
            .game-message {
                position: fixed !important;
                top: 0 !important;
                left: 0 !important;
                width: 100vw !important;
                height: 100vh !important;
                background: rgba(0, 0, 0, 0.8) !important;
                display: none !important;
                justify-content: center !important;
                align-items: center !important;
                z-index: 10000 !important;
                pointer-events: none !important;
            }
            
            .game-message.show-modal {
                display: flex !important;
                pointer-events: auto !important;
            }
            
            .game-over-content {
                background: linear-gradient(135deg, rgba(26, 26, 58, 0.95), rgba(15, 15, 35, 0.95)) !important;
                border: 2px solid rgba(255, 215, 0, 0.5) !important;
                border-radius: 15px !important;
                padding: 40px !important;
                text-align: center !important;
                box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5) !important;
                max-width: 400px !important;
                width: 90% !important;
                pointer-events: auto !important;
                z-index: 10001 !important;
                position: relative !important;
            }
            
            .game-over-buttons {
                display: flex !important;
                gap: 15px !important;
                justify-content: center !important;
                margin-top: 30px !important;
                flex-wrap: wrap !important;
            }
            
            .game-over-buttons button {
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%) !important;
                border: none !important;
                color: white !important;
                padding: 12px 24px !important;
                border-radius: 8px !important;
                font-size: 16px !important;
                font-weight: bold !important;
                cursor: pointer !important;
                transition: all 0.3s ease !important;
                min-width: 120px !important;
                pointer-events: auto !important;
                z-index: 10002 !important;
                position: relative !important;
                touch-action: manipulation !important;
                user-select: none !important;
            }
            
            .game-over-buttons button:hover {
                transform: translateY(-2px) !important;
                box-shadow: 0 8px 25px rgba(0,0,0,0.3) !important;
            }
            
            .game-over-buttons button:active {
                transform: translateY(0) !important;
                box-shadow: 0 4px 15px rgba(0,0,0,0.2) !important;
            }
            
            .game-over-buttons button:focus {
                outline: 2px solid #fff !important;
                outline-offset: 2px !important;
            }
        `;
        document.head.appendChild(style);
    }
    
    setupModalEventHandling(gameMessage) {
        // Prevent event bubbling issues
        gameMessage.addEventListener('click', (e) => {
            if (e.target === gameMessage) {
                // Clicked on backdrop - don't close modal for game over
                e.stopPropagation();
            }
        });
        
        // Prevent scroll when modal is open
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                    if (gameMessage.classList.contains('show-modal')) {
                        document.body.style.overflow = 'hidden';
                    } else {
                        document.body.style.overflow = '';
                    }
                }
            });
        });
        
        observer.observe(gameMessage, { attributes: true });
    }
    
    // Setup bulletproof button handlers
    setupButtonHandlers() {
        this.setupRetryButton();
        this.setupHomeButton();
        this.setupPowerUpButtons();
    }
    
    setupRetryButton() {
        const handler = (e) => {
            e.preventDefault();
            e.stopPropagation();
            
            if (!window.gameStateManager.is('GAME_OVER')) return;
            
            console.log('Retry button clicked');
            if (window.gameSounds) {
                window.gameSounds.playSpawn(); // Use original sound system
            }
            
            // Hide modal first
            this.hideGameOverModal();
            
            // Set loading state
            window.gameStateManager.setState('LOADING');
            
            // Restart game after brief delay
            setTimeout(() => {
                if (window.gameManager) {
                    window.gameManager.restart();
                }
                if (window.resetPowerUps) {
                    window.resetPowerUps();
                }
                window.gameStateManager.setState('PLAYING');
            }, 100);
        };
        
        this.addButtonHandler('retryBtn', handler);
        this.addButtonHandler('retry-button', handler, 'class');
    }
    
    setupHomeButton() {
        const handler = (e) => {
            e.preventDefault();
            e.stopPropagation();
            
            console.log('Home button clicked');
            if (window.gameSounds) {
                window.gameSounds.playSpawn(); // Use original sound system
            }
            
            // Hide modal
            this.hideGameOverModal();
            
            // Go to home
            if (window.goToHome) {
                window.goToHome();
            } else {
                this.showScreen('homeScreen');
            }
            
            window.gameStateManager.setState('IDLE');
        };
        
        this.addButtonHandler('homeBtn', handler);
    }
    
    setupPowerUpButtons() {
        // Undo button
        this.addButtonHandler('undo-btn', (e) => {
            if (!window.gameStateManager.is('PLAYING')) return;
            if (window.handleUndoClick) {
                window.handleUndoClick(e.target);
            }
        }, 'class');
        
        // Swap button
        this.addButtonHandler('replace-btn', (e) => {
            if (!window.gameStateManager.is('PLAYING')) return;
            if (window.handleReplaceClick) {
                window.handleReplaceClick(e.target);
            }
        }, 'class');
        
        // Delete button
        this.addButtonHandler('menu-btn', (e) => {
            if (!window.gameStateManager.is('PLAYING')) return;
            if (window.handleMenuClick) {
                window.handleMenuClick(e.target);
            }
        }, 'class');
    }
    
    // Add button handler with multiple fallback methods
    addButtonHandler(selector, handler, type = 'id') {
        const key = `${type}:${selector}`;
        this.buttonClickHandlers.set(key, handler);
        
        // Method 1: Direct element binding
        const element = type === 'id' ? 
            document.getElementById(selector) : 
            document.querySelector(`.${selector}`);
            
        if (element) {
            this.bindElementEvents(element, handler);
        }
        
        // Method 2: Event delegation (always active)
        // This is handled in setupGlobalEventDelegation
    }
    
    bindElementEvents(element, handler) {
        // Remove existing listeners to prevent duplicates
        const newElement = element.cloneNode(true);
        element.parentNode?.replaceChild(newElement, element);
        
        // Add multiple event types for maximum compatibility
        ['click', 'touchend', 'pointerup'].forEach(eventType => {
            newElement.addEventListener(eventType, handler, { passive: false });
        });
        
        // Add keyboard support
        newElement.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                handler(e);
            }
        });
    }
    
    // Global event delegation as fallback
    setupGlobalEventDelegation() {
        document.addEventListener('click', (e) => {
            this.handleGlobalClick(e);
        }, true); // Use capture phase
        
        document.addEventListener('touchend', (e) => {
            this.handleGlobalClick(e);
        }, true);
    }
    
    handleGlobalClick(e) {
        const target = e.target;
        
        // Check by ID
        if (target.id) {
            const handler = this.buttonClickHandlers.get(`id:${target.id}`);
            if (handler) {
                handler(e);
                return;
            }
        }
        
        // Check by class
        target.classList.forEach(className => {
            const handler = this.buttonClickHandlers.get(`class:${className}`);
            if (handler) {
                handler(e);
            }
        });
    }
    
    // Modal management
    showGameOverModal() {
        const gameMessage = document.querySelector('.game-message');
        if (!gameMessage) return;
        
        gameMessage.classList.add('game-over');
        gameMessage.classList.add('show-modal');
        gameMessage.style.display = 'flex';
        
        // Ensure buttons are properly set up
        setTimeout(() => {
            this.setupButtonHandlers();
        }, 100);
        
        this.modalStack.push('game-over');
    }
    
    hideGameOverModal() {
        const gameMessage = document.querySelector('.game-message');
        if (!gameMessage) return;
        
        gameMessage.classList.remove('show-modal');
        gameMessage.classList.remove('game-over');
        gameMessage.style.display = 'none';
        
        // Remove from modal stack
        const index = this.modalStack.indexOf('game-over');
        if (index > -1) {
            this.modalStack.splice(index, 1);
        }
    }
    
    // Screen management
    showScreen(screenId) {
        // Hide all screens
        document.querySelectorAll('.screen').forEach(screen => {
            screen.classList.remove('active');
        });
        
        // Show target screen
        const targetScreen = document.getElementById(screenId);
        if (targetScreen) {
            targetScreen.classList.add('active');
        }
    }
    
    // State-based UI updates
    setupStateBasedUI() {
        window.gameStateManager.onStateChange((newState, oldState) => {
            this.updateUIForState(newState, oldState);
        });
    }
    
    updateUIForState(newState, oldState) {
        const gameContainer = document.querySelector('.game-container');
        const powerUpButtons = document.querySelectorAll('.power-up-btn, .undo-btn, .replace-btn, .menu-btn');
        
        switch (newState) {
            case 'LOADING':
                if (gameContainer) gameContainer.style.pointerEvents = 'none';
                powerUpButtons.forEach(btn => {
                    btn.disabled = true;
                    btn.style.opacity = '0.5';
                });
                break;
                
            case 'PLAYING':
                if (gameContainer) gameContainer.style.pointerEvents = 'auto';
                powerUpButtons.forEach(btn => {
                    btn.disabled = false;
                    btn.style.opacity = '1';
                });
                break;
                
            case 'GAME_OVER':
                if (gameContainer) gameContainer.style.pointerEvents = 'none';
                powerUpButtons.forEach(btn => {
                    btn.disabled = true;
                    btn.style.opacity = '0.5';
                });
                // Show game over modal
                setTimeout(() => this.showGameOverModal(), 100);
                break;
                
            case 'PAUSED':
                if (gameContainer) gameContainer.style.pointerEvents = 'none';
                break;
                
            case 'IDLE':
                this.hideGameOverModal();
                break;
        }
    }
    
    // Emergency reset
    emergencyReset() {
        // Clear all modals
        this.modalStack = [];
        this.hideGameOverModal();
        
        // Re-enable UI
        const gameContainer = document.querySelector('.game-container');
        if (gameContainer) gameContainer.style.pointerEvents = 'auto';
        
        // Reset state
        window.gameStateManager.forceState('IDLE');
        
        console.log('UI emergency reset completed');
    }
}

// Initialize global UI manager
window.uiManager = new UIManager();