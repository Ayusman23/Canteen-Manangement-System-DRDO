import { Routes, Route } from 'react-router-dom';
import Navbar from './components/Navbar';
import ProtectedRoute from './components/ProtectedRoute';
import EmployeePortal from './pages/EmployeePortal';
import LoginPage from './pages/LoginPage';
import MyBookings from './pages/MyBookings';
import KitchenKiosk from './pages/KitchenKiosk';
import AdminAnalytics from './pages/AdminAnalytics';
import { Shield, Phone, MapPin } from 'lucide-react';
import './App.css';

function App() {
  return (
    <div className="app-shell">
      <Navbar />

      <main className="main-content">
        <Routes>
          <Route path="/" element={<EmployeePortal />} />
          <Route path="/login" element={<LoginPage />} />

          <Route
            path="/my-bookings"
            element={
              <ProtectedRoute allowedRoles={['Employee', 'CanteenManager', 'KitchenOperator']}>
                <MyBookings />
              </ProtectedRoute>
            }
          />

          <Route
            path="/kitchen-kiosk"
            element={
              <ProtectedRoute allowedRoles={['KitchenOperator', 'CanteenManager']}>
                <KitchenKiosk />
              </ProtectedRoute>
            }
          />

          <Route
            path="/admin/analytics"
            element={
              <ProtectedRoute allowedRoles={['CanteenManager']}>
                <AdminAnalytics />
              </ProtectedRoute>
            }
          />
        </Routes>
      </main>

      <footer className="site-footer">
        <div className="footer-container">
          <div className="footer-brand-col">
            <div className="footer-brand-title">
              <Shield size={20} color="var(--primary-gold)" /> DRDO CANTEEN MANAGEMENT SYSTEM
            </div>
            <p>
              Autonomous dining logistics and nutrition dispatching infrastructure for Defence Research & Development
              Organisation establishments.
            </p>
          </div>

          <div className="footer-links-col">
            <h5>Navigation & Operations</h5>
            <ul>
              <li><a href="/">Weekly Dynamic Menu</a></li>
              <li><a href="/my-bookings">Personnel Token Wallet</a></li>
              <li><a href="/kitchen-kiosk">Kitchen Kiosk Terminal</a></li>
              <li><a href="/admin/analytics">Logistics & Analytics</a></li>
            </ul>
          </div>

          <div className="footer-contact-col">
            <h5>Security & Support</h5>
            <p className="contact-line">
              <MapPin size={16} /> DRDO HQ, Rajaji Marg, New Delhi
            </p>
            <p className="contact-line">
              <Phone size={16} /> Ext: 2301-2233 / Canteen Control Room
            </p>
            <div className="footer-clearance-pill">
              SECURE SECTOR 4 ENCLAVE
            </div>
          </div>
        </div>

        <div className="footer-bottom">
          &copy; {new Date().getFullYear()} Defence Research & Development Organisation (DRDO). All rights reserved.
        </div>
      </footer>
    </div>
  );
}

export default App;
