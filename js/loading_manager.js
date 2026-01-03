/**
 * Loading Manager - Polished loading screens with engaging messages
 * Prevents wrong sounds and provides smooth transitions
 */

class LoadingManager {
    constructor() {
        this.loadingMessages = [
            "Preparing your challenge...",
            "Shuffling the digits...",
            "Powering up the grid...",
            "Calculating possibilities...",
            "Aligning the tiles...",
            "Charging the power-ups...",
            "Optimizing the board...",
            "Synchronizing the numbers...",
            "Initializing the matrix...",
            "Calibrating difficulty..."
        ];
        
        this.currentLoadingId = null;
        this.loadingElement = null;
        this.isLoading = false;
        
        this.createLoadingElement();
    }
    
    createLoadingElement() {
        // Remove existing loading element
        const existing = document.getElementById('game-loading-overlay');
        if (existing) {
            existing.remove();
        }
        
        // Create new loading overlay
        this.loadingElement = document.createElement('div');
        this.loadingElement.id = 'game-loading-overlay';
        this.loadingElement.innerHTML = `
            <div class="loading-content">
                <div class="loading-logo">2048</div>
                <div class="loading-text" id="loading-message">Loading...</div>
                <div class="loading-spinner">
                    <div class="spinner-ring"></div>
                    <div class="spinner-ring"></div>
                    <div class="spinner-ring"></div>
                </div>
                <div class="loading-progress">
                    <div class="progress-bar" id="loading-progress-bar"></div>
                </div>
            </div>
        `;
        
        // Add CSS
        this.addLoadingCSS();
        
        // Add to document
        document.body.appendChild(this.loadingElement);
    }
    
    addLoadingCSS() {
        const style = document.createElement('style');
        style.textContent = `
            #game-loading-overlay {
                position: fixed;
                top: 0;
                left: 0;
                width: 100vw;
                height: 100vh;
                background: linear-gradient(135deg, #1a1a3a 0%, #0f0f23 100%);
                display: none;
                justify-content: center;
                align-items: center;
                z-index: 20000;
                opacity: 0;
                transition: opacity 0.3s ease;
            }
            
            #game-loading-overlay.show {
                display: flex;
                opacity: 1;
            }
            
            .loading-content {
                text-align: center;
                color: white;
                max-width: 400px;
                padding: 40px;
            }
            
            .loading-logo {
                font-size: 80px;
                font-weight: bold;
                background: linear-gradient(45deg, #ffd700, #ffb347);
                -webkit-background-clip: text;
                -webkit-text-fill-color: transparent;
                background-clip: text;
                margin-bottom: 30px;
                animation: logoGlow 2s ease-in-out infinite alternate;
            }
            
            @keyframes logoGlow {
                from {
                    text-shadow: 0 0 20px rgba(255, 215, 0, 0.3);
                    transform: scale(1);
                }
                to {
                    text-shadow: 0 0 40px rgba(255, 215, 0, 0.6);
                    transform: scale(1.05);
                }
            }
            
            .loading-text {
                font-size: 18px;
                color: #e6e6fa;
                margin-bottom: 30px;
                opacity: 0.9;
                min-height: 25px;
                animation: textFade 3s ease-in-out infinite;
            }
            
            @keyframes textFade {
                0%, 100% { opacity: 0.9; }
                50% { opacity: 0.6; }
            }
            
            .loading-spinner {
                position: relative;
                width: 60px;
                height: 60px;
                margin: 0 auto 30px;
            }
            
            .spinner-ring {
                position: absolute;
                width: 100%;
                height: 100%;
                border: 3px solid transparent;
                border-top: 3px solid #ffd700;
                border-radius: 50%;
                animation: spin 1.5s linear infinite;
            }
            
            .spinner-ring:nth-child(2) {
                width: 80%;
                height: 80%;
                top: 10%;
                left: 10%;
                border-top-color: #ffb347;
                animation-duration: 2s;
                animation-direction: reverse;
            }
            
            .spinner-ring:nth-child(3) {
                width: 60%;
                height: 60%;
                top: 20%;
                left: 20%;
                border-top-color: #ff6b6b;
                animation-duration: 1s;
            }
            
            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
            
            .loading-progress {
                width: 100%;
                height: 4px;
                background: rgba(255, 255, 255, 0.1);
                border-radius: 2px;
                overflow: hidden;
            }
            
            .progress-bar {
                height: 100%;
                background: linear-gradient(90deg, #ffd700, #ffb347);
                border-radius: 2px;
                width: 0%;
                transition: width 0.3s ease;
                animation: progressGlow 2s ease-in-out infinite;
            }
            
            @keyframes progressGlow {
                0%, 100% { box-shadow: 0 0 10px rgba(255, 215, 0, 0.5); }
                50% { box-shadow: 0 0 20px rgba(255, 215, 0, 0.8); }
            }
            
            /* Mobile responsive */
            @media (max-width: 480px) {
                .loading-logo {
                    font-size: 60px;
                }
                
                .loading-text {
                    font-size: 16px;
                }
                
                .loading-content {
                    padding: 20px;
                }
            }
        `;
        document.head.appendChild(style);
    }
    
    // Show loading screen with animated message and progress
    show(options = {}) {
        if (this.isLoading) return this.currentLoadingId;
        
        const {
            message = this.getRandomMessage(),
            duration = 2000,
            showProgress = true
        } = options;
        
        this.isLoading = true;
        this.currentLoadingId = Date.now();
        
        // Set loading state
        window.gameStateManager.setState('LOADING');
        
        // Update message
        const messageElement = document.getElementById('loading-message');
        if (messageElement) {
            messageElement.textContent = message;
        }
        
        // Show overlay
        this.loadingElement.classList.add('show');
        
        // Animate progress if enabled
        if (showProgress) {
            this.animateProgress(duration);
        }
        
        // Auto-hide after duration
        if (duration > 0) {
            setTimeout(() => {
                this.hide(this.currentLoadingId);
            }, duration);
        }
        
        return this.currentLoadingId;
    }
    
    // Hide loading screen
    hide(loadingId = null) {
        // Only hide if this is the current loading or no ID specified
        if (loadingId && loadingId !== this.currentLoadingId) {
            return;
        }
        
        this.isLoading = false;
        this.currentLoadingId = null;
        
        // Hide overlay
        this.loadingElement.classList.remove('show');
        
        // Reset progress
        const progressBar = document.getElementById('loading-progress-bar');
        if (progressBar) {
            progressBar.style.width = '0%';
        }
        
        // Set playing state after loading
        setTimeout(() => {
            if (window.gameStateManager.is('LOADING')) {
                window.gameStateManager.setState('PLAYING');
            }
        }, 300);
    }
    
    // Animate progress bar
    animateProgress(duration) {
        const progressBar = document.getElementById('loading-progress-bar');
        if (!progressBar) return;
        
        let progress = 0;
        const steps = 50;
        const stepDuration = duration / steps;
        
        const animate = () => {
            if (!this.isLoading) return;
            
            progress += (100 / steps);
            progressBar.style.width = Math.min(progress, 100) + '%';
            
            if (progress < 100) {
                setTimeout(animate, stepDuration);
            }
        };
        
        animate();
    }
    
    // Get random loading message
    getRandomMessage() {
        return this.loadingMessages[Math.floor(Math.random() * this.loadingMessages.length)];
    }
    
    // Update message during loading
    updateMessage(message) {
        const messageElement = document.getElementById('loading-message');
        if (messageElement && this.isLoading) {
            messageElement.style.opacity = '0';
            setTimeout(() => {
                messageElement.textContent = message;
                messageElement.style.opacity = '0.9';
            }, 150);
        }
    }
    
    // Show loading for specific actions
    showForLevelStart(level) {
        const messages = [
            `Preparing Level ${level}...`,
            `Loading ${level}x${level} grid...`,
            `Initializing challenge...`
        ];
        
        return this.show({
            message: messages[Math.floor(Math.random() * messages.length)],
            duration: 1500
        });
    }
    
    showForRestart() {
        return this.show({
            message: "Restarting game...",
            duration: 1000
        });
    }
    
    showForPowerUpActivation(powerUpType) {
        const messages = {
            undo: "Rewinding time...",
            swap: "Rearranging tiles...",
            delete: "Removing tile..."
        };
        
        return this.show({
            message: messages[powerUpType] || "Activating power-up...",
            duration: 800,
            showProgress: false
        });
    }
    
    // Emergency hide
    forceHide() {
        this.isLoading = false;
        this.currentLoadingId = null;
        this.loadingElement.classList.remove('show');
        
        const progressBar = document.getElementById('loading-progress-bar');
        if (progressBar) {
            progressBar.style.width = '0%';
        }
    }
}

// Initialize global loading manager
window.loadingManager = new LoadingManager();