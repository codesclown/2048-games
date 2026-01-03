/**
 * Production Test Suite - Comprehensive testing for all game systems
 * Verifies that all critical issues have been resolved
 */

class ProductionTestSuite {
    constructor() {
        this.tests = [];
        this.results = [];
        this.isRunning = false;
    }
    
    // Add a test
    addTest(name, testFunction, description = '') {
        this.tests.push({
            name,
            testFunction,
            description,
            status: 'pending'
        });
    }
    
    // Run all tests
    async runAllTests() {
        if (this.isRunning) {
            console.warn('Tests already running');
            return;
        }
        
        this.isRunning = true;
        this.results = [];
        
        console.log('🧪 Starting Production Test Suite...');
        console.log(`📊 Running ${this.tests.length} tests`);
        
        for (const test of this.tests) {
            await this.runTest(test);
        }
        
        this.isRunning = false;
        this.generateReport();
    }
    
    // Run a single test
    async runTest(test) {
        console.log(`🔍 Testing: ${test.name}`);
        
        try {
            const startTime = Date.now();
            const result = await test.testFunction();
            const duration = Date.now() - startTime;
            
            test.status = result.success ? 'passed' : 'failed';
            test.result = result;
            test.duration = duration;
            
            this.results.push({
                name: test.name,
                success: result.success,
                message: result.message,
                duration: duration,
                details: result.details || {}
            });
            
            const status = result.success ? '✅' : '❌';
            console.log(`${status} ${test.name}: ${result.message} (${duration}ms)`);
            
        } catch (error) {
            test.status = 'error';
            test.error = error;
            
            this.results.push({
                name: test.name,
                success: false,
                message: `Test error: ${error.message}`,
                duration: 0,
                error: error
            });
            
            console.error(`💥 ${test.name}: Test error -`, error);
        }
    }
    
    // Generate test report
    generateReport() {
        const passed = this.results.filter(r => r.success).length;
        const failed = this.results.filter(r => !r.success).length;
        const total = this.results.length;
        
        console.log('\n📋 TEST REPORT');
        console.log('='.repeat(50));
        console.log(`Total Tests: ${total}`);
        console.log(`✅ Passed: ${passed}`);
        console.log(`❌ Failed: ${failed}`);
        console.log(`📊 Success Rate: ${((passed / total) * 100).toFixed(1)}%`);
        
        if (failed > 0) {
            console.log('\n❌ FAILED TESTS:');
            this.results.filter(r => !r.success).forEach(result => {
                console.log(`  • ${result.name}: ${result.message}`);
            });
        }
        
        console.log('\n' + '='.repeat(50));
        
        return {
            total,
            passed,
            failed,
            successRate: (passed / total) * 100,
            results: this.results
        };
    }
}

// Initialize test suite
const testSuite = new ProductionTestSuite();

// ===== CRITICAL SYSTEM TESTS =====

// Test 1: Game State Manager
testSuite.addTest('Game State Manager', async () => {
    if (!window.gameStateManager) {
        return { success: false, message: 'Game State Manager not found' };
    }
    
    // Test state transitions
    const initialState = window.gameStateManager.getState();
    const canTransition = window.gameStateManager.setState('LOADING');
    const currentState = window.gameStateManager.getState();
    
    // Reset to initial state
    window.gameStateManager.setState(initialState);
    
    return {
        success: canTransition && currentState === 'LOADING',
        message: canTransition ? 'State management working' : 'State transition failed',
        details: { initialState, currentState, canTransition }
    };
});

// Test 2: Sound Manager
testSuite.addTest('Sound Manager', async () => {
    if (!window.soundManager) {
        return { success: false, message: 'Sound Manager not found' };
    }
    
    const initialEnabled = window.soundManager.enabled;
    
    // Test sound controls
    window.soundManager.mute();
    const isMuted = !window.soundManager.enabled;
    
    window.soundManager.unmute();
    const isUnmuted = window.soundManager.enabled;
    
    // Test state-based sound blocking
    window.gameStateManager.setState('LOADING');
    const shouldBlock = !window.soundManager.shouldPlaySound('slide');
    
    // Reset
    window.gameStateManager.setState('PLAYING');
    window.soundManager.enabled = initialEnabled;
    
    return {
        success: isMuted && isUnmuted && shouldBlock,
        message: 'Sound management working',
        details: { isMuted, isUnmuted, shouldBlock }
    };
});

// Test 3: UI Manager
testSuite.addTest('UI Manager', async () => {
    if (!window.uiManager) {
        return { success: false, message: 'UI Manager not found' };
    }
    
    // Test modal management
    const gameMessage = document.querySelector('.game-message');
    if (!gameMessage) {
        return { success: false, message: 'Game message element not found' };
    }
    
    // Test show/hide modal
    window.uiManager.showGameOverModal();
    const isShown = gameMessage.classList.contains('show-modal');
    
    window.uiManager.hideGameOverModal();
    const isHidden = !gameMessage.classList.contains('show-modal');
    
    return {
        success: isShown && isHidden,
        message: 'UI management working',
        details: { isShown, isHidden }
    };
});

// Test 4: Loading Manager
testSuite.addTest('Loading Manager', async () => {
    if (!window.loadingManager) {
        return { success: false, message: 'Loading Manager not found' };
    }
    
    // Test loading screen
    const loadingId = window.loadingManager.show({ duration: 100 });
    const isLoading = window.loadingManager.isLoading;
    
    // Wait for auto-hide
    await new Promise(resolve => setTimeout(resolve, 150));
    const isHidden = !window.loadingManager.isLoading;
    
    return {
        success: isLoading && isHidden,
        message: 'Loading management working',
        details: { loadingId, isLoading, isHidden }
    };
});

// Test 5: Tile Sync Manager
testSuite.addTest('Tile Sync Manager', async () => {
    if (!window.tileSyncManager) {
        return { success: false, message: 'Tile Sync Manager not found' };
    }
    
    const stats = window.tileSyncManager.getStats();
    const hasStats = stats && typeof stats.lastSyncTime === 'number';
    
    return {
        success: hasStats,
        message: 'Tile sync management working',
        details: stats
    };
});

// Test 6: Game Over Buttons
testSuite.addTest('Game Over Buttons', async () => {
    const retryBtn = document.getElementById('retryBtn');
    const homeBtn = document.getElementById('homeBtn');
    
    if (!retryBtn || !homeBtn) {
        return { 
            success: false, 
            message: 'Game over buttons not found',
            details: { retryFound: !!retryBtn, homeFound: !!homeBtn }
        };
    }
    
    // Test button properties
    const retryClickable = !retryBtn.disabled && retryBtn.style.pointerEvents !== 'none';
    const homeClickable = !homeBtn.disabled && homeBtn.style.pointerEvents !== 'none';
    
    // Test button handlers exist
    const retryHasHandler = typeof retryBtn.onclick === 'function' || 
                           window.uiManager.buttonClickHandlers.has('id:retryBtn');
    const homeHasHandler = typeof homeBtn.onclick === 'function' || 
                          window.uiManager.buttonClickHandlers.has('id:homeBtn');
    
    return {
        success: retryClickable && homeClickable && retryHasHandler && homeHasHandler,
        message: 'Game over buttons working',
        details: { retryClickable, homeClickable, retryHasHandler, homeHasHandler }
    };
});

// Test 7: Power-Up System Integration
testSuite.addTest('Power-Up System', async () => {
    if (!window.powerUpSystem) {
        return { success: false, message: 'Power-Up System not found' };
    }
    
    // Test initialization
    window.powerUpSystem.initialize();
    const hasState = window.powerUpState && typeof window.powerUpState.moveCount === 'number';
    
    // Test legacy compatibility
    window.powerUpSystem.updateLegacy();
    const hasLegacy = window.powerUps && typeof window.powerUps.undo === 'object';
    
    return {
        success: hasState && hasLegacy,
        message: 'Power-up system working',
        details: { hasState, hasLegacy }
    };
});

// Test 8: Game Manager Integration
testSuite.addTest('Game Manager Integration', async () => {
    if (!window.GameManager) {
        return { success: false, message: 'GameManager class not found' };
    }
    
    // Test game manager creation
    const testManager = new GameManager(4, KeyboardInputManager, HTMLActuator, LocalStorageManager);
    const hasGrid = testManager.grid && typeof testManager.grid.size === 'number';
    const hasScore = typeof testManager.score === 'number';
    const hasMethods = typeof testManager.move === 'function' && 
                      typeof testManager.restart === 'function';
    
    return {
        success: hasGrid && hasScore && hasMethods,
        message: 'Game manager integration working',
        details: { hasGrid, hasScore, hasMethods }
    };
});

// Test 9: State-Based Sound Blocking
testSuite.addTest('State-Based Sound Blocking', async () => {
    if (!window.soundManager || !window.gameStateManager) {
        return { success: false, message: 'Required managers not found' };
    }
    
    // Test sound blocking during loading
    window.gameStateManager.setState('LOADING');
    const blockedDuringLoading = !window.soundManager.shouldPlaySound('spawn');
    
    // Test sound allowed during playing
    window.gameStateManager.setState('PLAYING');
    const allowedDuringPlaying = window.soundManager.shouldPlaySound('spawn');
    
    // Test sound blocked during game over
    window.gameStateManager.setState('GAME_OVER');
    const blockedDuringGameOver = !window.soundManager.shouldPlaySound('spawn');
    
    // Reset to playing
    window.gameStateManager.setState('PLAYING');
    
    return {
        success: blockedDuringLoading && allowedDuringPlaying && blockedDuringGameOver,
        message: 'State-based sound blocking working',
        details: { blockedDuringLoading, allowedDuringPlaying, blockedDuringGameOver }
    };
});

// Test 10: Emergency Reset
testSuite.addTest('Emergency Reset', async () => {
    if (!window.emergencyReset) {
        return { success: false, message: 'Emergency reset function not found' };
    }
    
    // Mess up the state
    window.gameStateManager.setState('GAME_OVER');
    window.loadingManager.show({ duration: 0 });
    
    // Test emergency reset
    window.emergencyReset();
    
    // Check if state is reset
    const stateReset = window.gameStateManager.getState() === 'IDLE';
    const loadingReset = !window.loadingManager.isLoading;
    
    return {
        success: stateReset && loadingReset,
        message: 'Emergency reset working',
        details: { stateReset, loadingReset }
    };
});

// Export test suite
window.productionTestSuite = testSuite;

// Auto-run tests when loaded (optional)
if (typeof window !== 'undefined' && window.location?.search?.includes('autotest')) {
    setTimeout(() => {
        testSuite.runAllTests();
    }, 2000);
}