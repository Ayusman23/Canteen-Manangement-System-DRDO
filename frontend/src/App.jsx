import { useState, useEffect } from 'react'
import axios from 'axios'
import { Utensils, Clock, MapPin, Phone, ShoppingBag, Menu as MenuIcon, X, CheckCircle, AlertTriangle } from 'lucide-react'
import { motion, AnimatePresence } from 'framer-motion'
import './App.css'

const API_BASE_URL = 'http://localhost:5170/api/canteen'

function App() {
  const [menu, setMenu] = useState([])
  const [todayMenu, setTodayMenu] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [bookingStatus, setBookingStatus] = useState(null)
  const [formData, setFormData] = useState({
    name: '',
    mealType: 'Normal',
    day: new Date().toLocaleDateString('en-US', { weekday: 'long' })
  })

  useEffect(() => {
    fetchData()
  }, [])

  const fetchData = async () => {
    try {
      setLoading(true)
      setError(null)

      // Fetch menu
      const menuRes = await axios.get(`${API_BASE_URL}/menu`).catch(err => {
        console.error("Menu fetch failed:", err)
        return { data: [] }
      })

      const menuData = Array.isArray(menuRes.data) ? menuRes.data : []
      setMenu(menuData)

      // Fetch today's menu
      try {
        const todayRes = await axios.get(`${API_BASE_URL}/today`)
        setTodayMenu(todayRes.data)
      } catch (err) {
        console.warn("Today's menu not available")
      }

      if (menuData.length === 0) {
        setError("Could not load menu. Please ensure the backend server is running.")
      }
    } catch (error) {
      console.error("Error fetching data:", error)
      setError("Failed to connect to the server.")
    } finally {
      setLoading(false)
    }
  }

  const handleBooking = async (e) => {
    e.preventDefault()
    try {
      const res = await axios.post(`${API_BASE_URL}/book`, formData)
      setBookingStatus(res.data)
      setIsModalOpen(false)
      setTimeout(() => setBookingStatus(null), 8000)
    } catch (error) {
      console.error("Booking error:", error)
      alert("Booking failed. Please check if the backend is running.")
    }
  }

  return (
    <div className="app-container">
      <header>
        <div className="logo">
          <Utensils size={32} />
          DRDO <span>Canteen</span>
        </div>
        <nav>
          <ul>
            <li><a href="#menu">Weekly Menu</a></li>
            <li><a href="#about">About</a></li>
            <li><a href="#contact">Contact</a></li>
          </ul>
        </nav>
        <button className="btn-primary" onClick={() => setIsModalOpen(true)}>Book a Meal</button>
      </header>

      <main>
        <section className="hero">
          <motion.div
            className="hero-content"
            initial={{ opacity: 0, x: -50 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.8 }}
          >
            <span className="badge">Defence Research & Development Organisation</span>
            <h1>Premium Quality Meals for <span>DRDO Professionals</span></h1>
            <p>Experience the finest Indian cuisine prepared daily with the highest hygiene standards for our dedicated scientists and staff.</p>
            <div className="cta-group">
              <button className="btn-primary" onClick={() => setIsModalOpen(true)}>Order Now</button>
              <a href="#menu" className="btn-secondary">View Menu</a>
            </div>
          </motion.div>
          <motion.div
            className="hero-image"
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.8 }}
          >
            <img src="https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80" alt="Special Thali" />
          </motion.div>
        </section>

        {error && (
          <div style={{ background: '#fef2f2', border: '1px solid #fee2e2', color: '#b91c1c', padding: '1rem', margin: '0 5% 2rem', borderRadius: '12px', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <AlertTriangle size={24} />
            <div>
              <strong>Backend Connection Issue:</strong> {error}
              <button onClick={fetchData} style={{ marginLeft: '12px', background: 'none', border: 'underline', cursor: 'pointer', color: '#b91c1c', fontWeight: 'bold' }}>Retry</button>
            </div>
          </div>
        )}

        {bookingStatus && (
          <motion.div
            className="success-bar"
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            style={{ background: '#dcfce7', color: '#166534', padding: '16px', margin: '0 5% 2rem', borderRadius: '12px', textAlign: 'center', fontWeight: 'bold', boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}
          >
            <CheckCircle size={20} style={{ verticalAlign: 'middle', marginRight: '8px' }} />
            {bookingStatus.message} Your token is: <span style={{ fontSize: '1.2rem', marginLeft: '8px', color: '#1e40af' }}>{bookingStatus.token}</span>
          </motion.div>
        )}

        <section id="menu" className="menu-section">
          <div className="section-title">
            <h2>Weekly Specials</h2>
            <p>Check out our curated menu for the week</p>
          </div>

          <div className="menu-grid">
            {loading ? (
              <div style={{ textAlign: 'center', gridColumn: '1/-1', padding: '40px' }}>
                <Clock className="animate-spin" style={{ margin: '0 auto 12px' }} />
                <p>Fetching the latest menu...</p>
              </div>
            ) : (
              (Array.isArray(menu) && menu.length > 0) ? menu.map((item, idx) => (
                <motion.div
                  key={idx}
                  className="menu-card"
                  initial={{ opacity: 0, y: 20 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true }}
                  transition={{ delay: idx * 0.1 }}
                >
                  <div className="day-badge">{item?.day}</div>
                  <h3>Lunch Specialties</h3>
                  <div className="meal-option">
                    <span className="meal-name">Normal: {item?.normal?.meal}</span>
                    <span className="meal-price">₹{item?.normal?.price}</span>
                  </div>
                  <div className="meal-option">
                    <span className="meal-name">Special: {item?.special?.meal}</span>
                    <span className="meal-price">₹{item?.special?.price}</span>
                  </div>
                  <div className="meal-option">
                    <span className="meal-name">Add-on: {item?.item?.name}</span>
                    <span className="meal-price">₹{item?.item?.price}</span>
                  </div>
                  <div style={{ marginTop: '20px' }}>
                    <button
                      className="btn-secondary"
                      style={{ width: '100%' }}
                      onClick={() => {
                        setFormData({ ...formData, day: item.day })
                        setIsModalOpen(true)
                      }}
                    >
                      Pre-book for {item.day}
                    </button>
                  </div>
                </motion.div>
              )) : (
                <div style={{ gridColumn: '1/-1', textAlign: 'center', padding: '40px', color: '#64748b' }}>
                  No menu data available.
                </div>
              )
            )}
          </div>
        </section>
      </main>

      <footer>
        <div className="footer-content">
          <div className="footer-info">
            <div className="footer-logo">DRDO Canteen</div>
            <p style={{ marginTop: '16px', color: '#94a3b8', maxWidth: '300px' }}>
              Official canteen management system for Defence Research & Development Organisation personnel.
            </p>
          </div>
          <div className="footer-links">
            <div className="footer-column">
              <h4>Resources</h4>
              <ul>
                <li><a href="#">Dietary Info</a></li>
                <li><a href="#">Hygiene Polices</a></li>
                <li><a href="#">Support</a></li>
              </ul>
            </div>
            <div className="footer-column">
              <h4>Contact</h4>
              <ul style={{ color: '#94a3b8' }}>
                <li style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}><MapPin size={16} /> DRDO HQ, New Delhi</li>
                <li style={{ display: 'flex', alignItems: 'center', gap: '8px' }}><Phone size={16} /> +91 11 2301 2233</li>
              </ul>
            </div>
          </div>
        </div>
        <div className="footer-bottom">
          &copy; 2025 DRDO Canteen Management System. All rights reserved.
        </div>
      </footer>

      <AnimatePresence>
        {isModalOpen && (
          <div className="modal-overlay" onClick={() => setIsModalOpen(false)}>
            <motion.div
              className="modal"
              onClick={e => e.stopPropagation()}
              initial={{ scale: 0.9, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.9, opacity: 0 }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '20px' }}>
                <h2>Meal Booking</h2>
                <X size={24} style={{ cursor: 'pointer' }} onClick={() => setIsModalOpen(false)} />
              </div>
              <form onSubmit={handleBooking}>
                <div className="form-group">
                  <label>Full Name</label>
                  <input
                    type="text"
                    placeholder="Enter your name"
                    required
                    value={formData.name}
                    onChange={e => setFormData({ ...formData, name: e.target.value })}
                  />
                </div>
                <div className="form-group">
                  <label>Booking Day</label>
                  <select
                    value={formData.day}
                    onChange={e => setFormData({ ...formData, day: e.target.value })}
                  >
                    {Array.isArray(menu) && menu.map(m => <option key={m.day} value={m.day}>{m.day}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label>Meal Type</label>
                  <select
                    value={formData.mealType}
                    onChange={e => setFormData({ ...formData, mealType: e.target.value })}
                  >
                    <option value="Normal">Normal Thali (₹30)</option>
                    <option value="Special">Special Thali (₹50)</option>
                  </select>
                </div>
                <button type="submit" className="btn-primary" style={{ width: '100%', marginTop: '10px' }}>
                  Confirm Booking
                </button>
              </form>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  )
}

export default App
