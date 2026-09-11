import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Utensils, Shield, QrCode, BarChart3, LogOut, LogIn, User, Radio } from 'lucide-react';

export default function Navbar() {
  const { user, isAuthenticated, logout, isKitchenOperator, isManager } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <header className="site-header">
      <div className="header-container">
        <Link to="/" className="brand-logo">
          <div className="logo-badge">
            <Utensils size={22} />
          </div>
          <div className="brand-text">
            <span className="brand-title">DRDO <span>CANTEEN</span></span>
            <span className="brand-subtitle">DEFENCE CATERING SYSTEM</span>
          </div>
        </Link>

        <nav className="nav-menu">
          <Link to="/" className={`nav-link ${location.pathname === '/' ? 'active' : ''}`}>
            Weekly Menu
          </Link>

          {isAuthenticated && (
            <Link to="/my-bookings" className={`nav-link ${location.pathname === '/my-bookings' ? 'active' : ''}`}>
              My Tokens
            </Link>
          )}

          {isKitchenOperator && (
            <Link to="/kitchen-kiosk" className={`nav-link highlight-kiosk ${location.pathname === '/kitchen-kiosk' ? 'active' : ''}`}>
              <QrCode size={16} />
              Kitchen Kiosk
            </Link>
          )}

          {isManager && (
            <Link to="/admin/analytics" className={`nav-link highlight-admin ${location.pathname === '/admin/analytics' ? 'active' : ''}`}>
              <BarChart3 size={16} />
              Analytics & Ops
            </Link>
          )}
        </nav>

        <div className="header-actions">
          <div className="live-indicator" title="Connected to Real-time SignalR Telemetry">
            <span className="pulse-dot"></span>
            <span className="live-text">LIVE FEED</span>
          </div>

          {isAuthenticated ? (
            <div className="user-profile-widget">
              <div className="user-info">
                <span className="user-name">{user?.fullName}</span>
                <span className="user-role-badge">{user?.role}</span>
              </div>
              <button onClick={handleLogout} className="btn-icon" title="Logout">
                <LogOut size={18} />
              </button>
            </div>
          ) : (
            <Link to="/login" className="btn-login">
              <LogIn size={16} />
              <span>Portal Login</span>
            </Link>
          )}
        </div>
      </div>
    </header>
  );
}
