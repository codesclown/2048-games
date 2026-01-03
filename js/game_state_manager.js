/**
 * Game State Manager - Centralized state management for production stability
 * Handles all game states and prevents race conditions
 */

class GameStateManager {
    constructor() {
        this.state = 'IDLE';
        this.previousState = null;
        this.stateChangeListeners = [];
        this.allowedTransitions = {
            'IDLE': ['LOADING', 'PLAYING'],
            'LOADING': ['PLAYING', 'ERROR'],
            'PLAYING': ['PAUSED', 'GAME_OVER', 'LOADING'],
            'PAUSED': ['PLAYING', 'LOADING'],
            'GAME_OVER': ['LOADING', 'IDLE'],
            'ERROR': ['IDLE', 'LOADING']
        };
        
        // Prevent multiple simultaneous state changes
        this.stateChanging = false;
        this.pendingStateChange = null;
    }
    
    // Get current state
    getState() {
        return this.state;
    }
    
    // Check if in specific state
    is(state) {
        return this.state === state;
    }
    
    // Check if state change is allowed
    canTransitionTo(newState) {
        return this.allowedTransitions[this.state]?.includes(newState) || false;
    }
    
    // Change state with validation
    setState(newState, data = {}) {
        // Prevent recursive state changes
        if (this.stateChanging) {
            this.pendingStateChange = { state: newState, data };
            return false;
        }
        
        // Validate transition
        if (!this.canTransitionTo(newState)) {
            console.warn(`Invalid state transition: ${this.state} -> ${newState}`);
            return false;
        }
        
        this.stateChanging = true;
        this.previousState = this.state;
        this.state = newState;
        
        // Notify listeners
        this.notifyStateChange(newState, this.previousState, data);
        
        this.stateChanging = false;
        
        // Process pending state change if any
        if (this.pendingStateChange) {
            const pending = this.pendingStateChange;
            this.pendingStateChange = null;
            this.setState(pending.state, pending.data);
        }
        
        return true;
    }
    
    // Add state change listener
    onStateChange(callback) {
        this.stateChangeListeners.push(callback);
    }
    
    // Remove state change listener
    removeStateChangeListener(callback) {
        const index = this.stateChangeListeners.indexOf(callback);
        if (index > -1) {
            this.stateChangeListeners.splice(index, 1);
        }
    }
    
    // Notify all listeners of state change
    notifyStateChange(newState, oldState, data) {
        this.stateChangeListeners.forEach(callback => {
            try {
                callback(newState, oldState, data);
            } catch (error) {
                console.error('State change listener error:', error);
            }
        });
    }
    
    // Force state (emergency use only)
    forceState(newState) {
        console.warn(`Force setting state to: ${newState}`);
        this.previousState = this.state;
        this.state = newState;
        this.stateChanging = false;
        this.pendingStateChange = null;
    }
    
    // Reset to initial state
    reset() {
        this.forceState('IDLE');
    }
}

// Global game state manager
window.gameStateManager = new GameStateManager();

// State-based action guards
window.gameStateManager.onStateChange((newState, oldState) => {
    console.log(`Game state: ${oldState} -> ${newState}`);
    
    // Disable/enable UI based on state
    const gameContainer = document.querySelector('.game-container');
    const powerUpButtons = document.querySelectorAll('.power-up-btn');
    
    switch (newState) {
        case 'LOADING':
            if (gameContainer) gameContainer.style.pointerEvents = 'none';
            powerUpButtons.forEach(btn => btn.disabled = true);
            break;
            
        case 'PLAYING':
            if (gameContainer) gameContainer.style.pointerEvents = 'auto';
            powerUpButtons.forEach(btn => btn.disabled = false);
            break;
            
        case 'GAME_OVER':
            if (gameContainer) gameContainer.style.pointerEvents = 'none';
            powerUpButtons.forEach(btn => btn.disabled = true);
            break;
            
        case 'PAUSED':
            if (gameContainer) gameContainer.style.pointerEvents = 'none';
            break;
    }
});