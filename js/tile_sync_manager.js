/**
 * Tile Synchronization Manager - Ensures grid state and UI are always in sync
 * Prevents ghost tiles, duplicate tiles, and overlap bugs
 */

class TileSyncManager {
    constructor() {
        this.syncQueue = [];
        this.isProcessing = false;
        this.lastSyncTime = 0;
        this.syncInterval = null;
        this.debugMode = false;
        
        // Track tile states for validation
        this.expectedTiles = new Map();
        this.actualTiles = new Map();
        
        this.initialize();
    }
    
    initialize() {
        // Start periodic sync validation
        this.startSyncMonitoring();
        
        // Hook into game manager for sync points
        this.hookGameManager();
        
        console.log('Tile Sync Manager initialized');
    }
    
    // Start monitoring for desync issues
    startSyncMonitoring() {
        this.syncInterval = setInterval(() => {
            if (window.gameStateManager?.is('PLAYING')) {
                this.validateSync();
            }
        }, 1000); // Check every second during gameplay
    }
    
    // Hook into game manager methods
    hookGameManager() {
        if (!window.gameManager) {
            setTimeout(() => this.hookGameManager(), 100);
            return;
        }
        
        // Hook actuate method
        const originalActuate = window.gameManager.actuate;
        window.gameManager.actuate = (...args) => {
            const result = originalActuate.apply(window.gameManager, args);
            this.queueSync('actuate');
            return result;
        };
        
        // Hook move method
        const originalMove = window.gameManager.move;
        window.gameManager.move = (...args) => {
            this.capturePreMoveState();
            const result = originalMove.apply(window.gameManager, args);
            this.queueSync('move');
            return result;
        };
        
        // Hook restart method
        const originalRestart = window.gameManager.restart;
        window.gameManager.restart = (...args) => {
            const result = originalRestart.apply(window.gameManager, args);
            this.queueSync('restart');
            return result;
        };
    }
    
    // Capture state before move for validation
    capturePreMoveState() {
        if (!window.gameManager?.grid) return;
        
        this.expectedTiles.clear();
        window.gameManager.grid.eachCell((x, y, tile) => {
            if (tile) {
                const key = `${x}-${y}`;
                this.expectedTiles.set(key, {
                    x: tile.x,
                    y: tile.y,
                    value: tile.value,
                    id: tile.id || `${x}-${y}-${tile.value}`
                });
            }
        });
    }
    
    // Queue a sync operation
    queueSync(reason) {
        this.syncQueue.push({
            reason,
            timestamp: Date.now()
        });
        
        if (!this.isProcessing) {
            this.processSync();
        }
    }
    
    // Process sync queue
    async processSync() {
        if (this.isProcessing || this.syncQueue.length === 0) return;
        
        this.isProcessing = true;
        
        while (this.syncQueue.length > 0) {
            const syncOp = this.syncQueue.shift();
            await this.performSync(syncOp);
        }
        
        this.isProcessing = false;
    }
    
    // Perform actual synchronization
    async performSync(syncOp) {
        if (!window.gameManager?.grid) return;
        
        try {
            // Capture current UI state
            this.captureUIState();
            
            // Validate grid state
            const validation = this.validateGridState();
            
            if (!validation.isValid) {
                console.warn('Grid desync detected:', validation.issues);
                this.fixDesyncIssues(validation.issues);
            }
            
            // Ensure UI matches grid
            this.syncUIWithGrid();
            
            this.lastSyncTime = Date.now();
            
            if (this.debugMode) {
                console.log(`Sync completed: ${syncOp.reason}`);
            }
            
        } catch (error) {
            console.error('Sync error:', error);
            this.emergencySync();
        }
    }
    
    // Capture current UI tile state
    captureUIState() {
        this.actualTiles.clear();
        
        const tileElements = document.querySelectorAll('.tile');
        tileElements.forEach(element => {
            const classes = Array.from(element.classList);
            const positionClass = classes.find(c => c.startsWith('tile-position-'));
            const valueClass = classes.find(c => c.startsWith('tile-') && c !== 'tile' && !c.startsWith('tile-position-'));
            
            if (positionClass && valueClass) {
                const position = this.parsePositionClass(positionClass);
                const value = this.parseValueClass(valueClass);
                
                if (position && value) {
                    const key = `${position.x}-${position.y}`;
                    this.actualTiles.set(key, {
                        x: position.x,
                        y: position.y,
                        value: value,
                        element: element
                    });
                }
            }
        });
    }
    
    // Parse position class (e.g., "tile-position-2-3" -> {x: 1, y: 2})
    parsePositionClass(className) {
        const match = className.match(/tile-position-(\d+)-(\d+)/);
        if (match) {
            return {
                x: parseInt(match[1]) - 1, // Convert from 1-based to 0-based
                y: parseInt(match[2]) - 1
            };
        }
        return null;
    }
    
    // Parse value class (e.g., "tile-128" -> 128)
    parseValueClass(className) {
        const match = className.match(/tile-(\d+)/);
        return match ? parseInt(match[1]) : null;
    }
    
    // Validate grid state consistency
    validateGridState() {
        const issues = [];
        const gridTiles = new Map();
        
        // Collect grid tiles
        window.gameManager.grid.eachCell((x, y, tile) => {
            if (tile) {
                const key = `${x}-${y}`;
                gridTiles.set(key, tile);
            }
        });
        
        // Check for duplicate positions
        const positions = new Set();
        gridTiles.forEach((tile, key) => {
            const posKey = `${tile.x}-${tile.y}`;
            if (positions.has(posKey)) {
                issues.push({
                    type: 'duplicate_position',
                    position: { x: tile.x, y: tile.y },
                    tile: tile
                });
            }
            positions.add(posKey);
        });
        
        // Check for tiles outside grid bounds
        gridTiles.forEach((tile, key) => {
            if (tile.x < 0 || tile.x >= window.gameManager.size ||
                tile.y < 0 || tile.y >= window.gameManager.size) {
                issues.push({
                    type: 'out_of_bounds',
                    position: { x: tile.x, y: tile.y },
                    tile: tile
                });
            }
        });
        
        // Check for invalid values
        gridTiles.forEach((tile, key) => {
            if (!tile.value || tile.value < 2 || !this.isPowerOfTwo(tile.value)) {
                issues.push({
                    type: 'invalid_value',
                    position: { x: tile.x, y: tile.y },
                    tile: tile,
                    value: tile.value
                });
            }
        });
        
        return {
            isValid: issues.length === 0,
            issues: issues,
            gridTiles: gridTiles
        };
    }
    
    // Check if number is power of 2
    isPowerOfTwo(n) {
        return n > 0 && (n & (n - 1)) === 0;
    }
    
    // Fix desync issues
    fixDesyncIssues(issues) {
        issues.forEach(issue => {
            switch (issue.type) {
                case 'duplicate_position':
                    this.fixDuplicatePosition(issue);
                    break;
                case 'out_of_bounds':
                    this.fixOutOfBounds(issue);
                    break;
                case 'invalid_value':
                    this.fixInvalidValue(issue);
                    break;
            }
        });
    }
    
    // Fix duplicate position issue
    fixDuplicatePosition(issue) {
        console.warn('Fixing duplicate position:', issue.position);
        
        // Remove the duplicate tile from grid
        window.gameManager.grid.removeTile(issue.tile);
        
        // Find an empty cell and place it there, or remove it entirely
        const availableCells = window.gameManager.grid.availableCells();
        if (availableCells.length > 0) {
            const newPosition = availableCells[0];
            issue.tile.x = newPosition.x;
            issue.tile.y = newPosition.y;
            window.gameManager.grid.insertTile(issue.tile);
        }
    }
    
    // Fix out of bounds tile
    fixOutOfBounds(issue) {
        console.warn('Fixing out of bounds tile:', issue.position);
        
        // Remove the tile
        window.gameManager.grid.removeTile(issue.tile);
    }
    
    // Fix invalid value
    fixInvalidValue(issue) {
        console.warn('Fixing invalid value:', issue.value);
        
        // Set to nearest valid value or remove
        if (issue.value < 2) {
            issue.tile.value = 2;
        } else {
            // Round to nearest power of 2
            issue.tile.value = Math.pow(2, Math.round(Math.log2(issue.value)));
        }
    }
    
    // Sync UI with grid state
    syncUIWithGrid() {
        if (!window.gameManager?.actuator) return;
        
        // Force a complete UI refresh
        const metadata = {
            score: window.gameManager.score,
            over: window.gameManager.over,
            won: window.gameManager.won,
            bestScore: window.gameManager.storageManager?.getBestScore() || 0,
            terminated: window.gameManager.isGameTerminated()
        };
        
        // Clear and rebuild UI
        window.gameManager.actuator.actuate(window.gameManager.grid, metadata);
    }
    
    // Validate sync between grid and UI
    validateSync() {
        if (!window.gameManager?.grid) return true;
        
        this.captureUIState();
        
        const gridTiles = new Map();
        window.gameManager.grid.eachCell((x, y, tile) => {
            if (tile) {
                const key = `${x}-${y}`;
                gridTiles.set(key, tile);
            }
        });
        
        // Check if UI matches grid
        let isInSync = true;
        const issues = [];
        
        // Check for missing UI tiles
        gridTiles.forEach((tile, key) => {
            if (!this.actualTiles.has(key)) {
                isInSync = false;
                issues.push(`Missing UI tile at ${key}: value ${tile.value}`);
            } else {
                const uiTile = this.actualTiles.get(key);
                if (uiTile.value !== tile.value) {
                    isInSync = false;
                    issues.push(`Value mismatch at ${key}: grid=${tile.value}, ui=${uiTile.value}`);
                }
            }
        });
        
        // Check for extra UI tiles
        this.actualTiles.forEach((uiTile, key) => {
            if (!gridTiles.has(key)) {
                isInSync = false;
                issues.push(`Extra UI tile at ${key}: value ${uiTile.value}`);
            }
        });
        
        if (!isInSync) {
            console.warn('Sync validation failed:', issues);
            this.queueSync('validation_failed');
        }
        
        return isInSync;
    }
    
    // Emergency sync - complete rebuild
    emergencySync() {
        console.warn('Performing emergency sync');
        
        if (!window.gameManager?.grid || !window.gameManager?.actuator) return;
        
        try {
            // Clear all UI tiles
            const tileContainer = document.querySelector('.tile-container');
            if (tileContainer) {
                while (tileContainer.firstChild) {
                    tileContainer.removeChild(tileContainer.firstChild);
                }
            }
            
            // Force complete rebuild
            this.syncUIWithGrid();
            
            console.log('Emergency sync completed');
            
        } catch (error) {
            console.error('Emergency sync failed:', error);
        }
    }
    
    // Enable debug mode
    enableDebug() {
        this.debugMode = true;
        console.log('Tile sync debug mode enabled');
    }
    
    // Disable debug mode
    disableDebug() {
        this.debugMode = false;
    }
    
    // Get sync statistics
    getStats() {
        return {
            lastSyncTime: this.lastSyncTime,
            queueLength: this.syncQueue.length,
            isProcessing: this.isProcessing,
            expectedTiles: this.expectedTiles.size,
            actualTiles: this.actualTiles.size
        };
    }
    
    // Cleanup
    destroy() {
        if (this.syncInterval) {
            clearInterval(this.syncInterval);
            this.syncInterval = null;
        }
        
        this.syncQueue = [];
        this.expectedTiles.clear();
        this.actualTiles.clear();
    }
}

// Initialize global tile sync manager
window.tileSyncManager = new TileSyncManager();