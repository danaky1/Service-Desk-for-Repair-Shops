<template>
  <div class="app">
    <!-- Модальное окно входа -->
    <div v-if="showLoginModal" class="modal-overlay">
      <div class="modal">
        <div class="modal-header">
          <h3>🔐 Вход в систему</h3>
          <button class="close-btn" @click="showLoginModal = false">×</button>
        </div>
        <div class="modal-content">
          <form @submit.prevent="login">
            <div class="form-group">
              <label>👤 Выберите мастера:</label>
              <select v-model="loginForm.masterId" required class="master-select">
                <option value="" disabled>Выберите мастера</option>
                <option 
                  v-for="master in availableMasters" 
                  :key="master.id" 
                  :value="master.id">
                  {{ master.name }} - {{ master.specialization }}
                </option>
              </select>
              <small class="demo-note">
                В демо-режиме пароль не требуется. Выбранный мастер будет загружен в систему.
              </small>
            </div>
            
            <div class="form-group" v-if="availableMasters.length === 0">
              <p class="error-text">
                ⚠️ Нет доступных мастеров. Пожалуйста, обновите страницу.
              </p>
            </div>
            
            <div class="form-actions">
              <button type="submit" class="submit-btn" :disabled="loggingIn || availableMasters.length === 0">
                {{ loggingIn ? 'Вход...' : 'Войти как выбранный мастер' }}
              </button>
            </div>
          </form>
          
          <div v-if="availableMasters.length > 0 && loginForm.masterId" class="master-preview">
            <h4>Выбранный мастер:</h4>
            <div>
              <p><strong>Имя:</strong> {{ availableMasters.find(m => m.id == loginForm.masterId)?.name }}</p>
              <p><strong>Специализация:</strong> {{ availableMasters.find(m => m.id == loginForm.masterId)?.specialization }}</p>
              <p><strong>Ставка:</strong> {{ availableMasters.find(m => m.id == loginForm.masterId)?.hourlyRate }} ₽/час</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Шапка -->
    <header class="header">
      <div class="header-left">
        <h1>ServiceDesk Pro</h1>
        <p>Система управления сервисным центром</p>
      </div>
      <div class="header-right">
        <div class="user-info">
          <span v-if="currentMaster">👨‍🔧 {{ currentMaster.name }}</span>
          <span v-else @click="showLoginModal = true" class="login-link">🔐 Войти</span>
          <button v-if="currentMaster" class="logout-btn" @click="logout">🚪 Выйти</button>
        </div>
      </div>
    </header>

    <!-- Основной контент -->
    <div v-if="currentMaster" class="main-content">
      <!-- Боковое меню -->
      <aside class="sidebar">
        <nav class="sidebar-nav">
          <button 
            v-for="tab in tabs" 
            :key="tab.id"
            :class="['nav-btn', { active: activeTab === tab.id }]"
            @click="switchTab(tab.id)"
          >
            {{ tab.name }}
          </button>
        </nav>
      </aside>

      <!-- Рабочая область -->
      <main class="workspace">
        <!-- Заголовок раздела -->
        <div class="workspace-header">
          <h2>{{ getActiveTabName() }}</h2>
          
          <!-- Кнопки действий -->
          <div class="action-buttons">
            <button v-if="activeTab === 'orders'" class="add-btn" @click="showNewOrderModal = true">
              📝 Новый заказ
            </button>
            <button v-if="activeTab === 'clients'" class="add-btn" @click="showNewClientModal = true">
              👤 Новый клиент
            </button>
            <button v-if="activeTab === 'parts'" class="add-btn" @click="showNewPartModal = true">
              🔩 Новая запчасть
            </button>
            <button v-if="activeTab === 'orders'" class="refresh-btn" @click="loadOrders">
              🔄 Обновить
            </button>
          </div>
        </div>

        <!-- Контент в зависимости от активной вкладки -->
        <div class="workspace-content">
          
          <!-- Вкладка Заказы -->
          <div v-if="activeTab === 'orders'">
            <!-- Фильтры -->
            <div class="filters">
              <select v-model="orderFilter.status" @change="filterOrders">
                <option value="">Все статусы</option>
                <option value="new">🆕 Новые</option>
                <option value="accepted">✅ Принятые</option>
                <option value="in_progress">🔧 В работе</option>
                <option value="waiting_parts">⏳ Ожидание запчастей</option>
                <option value="repair">🔧 Ремонт</option>
                <option value="ready">📦 Готов к выдаче</option>
                <option value="completed">🏁 Завершенные</option>
                <option value="cancelled">❌ Отмененные</option>
              </select>
              <select v-model="orderFilter.urgency" @change="filterOrders">
                <option value="">Все заказы</option>
                <option value="urgent">🚨 Только срочные</option>
              </select>
            </div>
            
            <!-- Статистика -->
            <div class="order-stats">
              <span class="stat-item">Всего: {{ filteredOrders.length }}</span>
              <span class="stat-item">🆕 Новые: {{ orders.filter(o => o.status === 'new').length }}</span>
              <span class="stat-item">🔧 В работе: {{ orders.filter(o => o.status === 'in_progress' || o.status === 'repair').length }}</span>
              <span class="stat-item">💰 Общая сумма: {{ totalOrdersAmount.toLocaleString('ru-RU') }} ₽</span>
            </div>
            
            <!-- Состояния загрузки/ошибок -->
            <div v-if="loading.orders" class="loading-state">
              <div class="spinner"></div>
              <p>Загрузка заказов...</p>
            </div>
            
            <div v-else-if="error.orders" class="error-state">
              <p>❌ Ошибка: {{ error.orders }}</p>
              <button @click="loadOrders">Повторить</button>
            </div>
            
            <!-- Список заказов -->
            <div v-else class="orders-grid">
              <div v-for="order in filteredOrders" :key="order.id" class="order-card">
                <div class="order-header">
                  <div class="order-number">
                    <strong>#{{ order.orderNumber }}</strong>
                    <span v-if="order.isUrgent" class="urgent-badge">🚨 Срочно</span>
                  </div>
                  <span :class="['status-badge', getStatusClass(order.status)]">
                    {{ getStatusText(order.status) }}
                  </span>
                </div>
                
                <div class="order-body">
                  <div class="client-info">
                    <h4>{{ order.clientName }}</h4>
                    <p class="client-phone">{{ order.clientPhone }}</p>
                  </div>
                  
                  <div class="device-info">
                    <p><strong>📱 Устройство:</strong> {{ order.device }}</p>
                    <p v-if="order.deviceModel"><strong>🔢 Модель:</strong> {{ order.deviceModel }}</p>
                    <p><strong>⚠️ Проблема:</strong> {{ truncateText(order.problemDescription, 60) }}</p>
                  </div>
                  
                  <div class="order-meta">
                    <div class="meta-item">
                      <span class="label">👨‍🔧 Мастер:</span>
                      <span>{{ order.masterName || 'Не назначен' }}</span>
                    </div>
                    <div class="meta-item">
                      <span class="label">📅 Создан:</span>
                      <span>{{ formatDate(order.createdDate) }}</span>
                    </div>
                    <div class="meta-item">
                      <span class="label">💰 Сумма:</span>
                      <span class="price">{{ order.totalAmount?.toLocaleString('ru-RU') || 0 }} ₽</span>
                    </div>
                    <div class="meta-item">
                      <span class="label">🛠️ Запчасти:</span>
                      <span>{{ order.partsCost?.toLocaleString('ru-RU') || 0 }} ₽</span>
                    </div>
                    <div class="meta-item">
                      <span class="label">⏱️ Работа:</span>
                      <span>{{ order.laborCost?.toLocaleString('ru-RU') || 0 }} ₽</span>
                    </div>
                  </div>
                </div>
                
                <div class="order-actions">
                  <button class="action-btn primary" @click="viewOrder(order.id)">
                    👁️ Просмотр
                  </button>
                  <button class="action-btn secondary" @click="openStatusModal(order)">
                    📝 Статус
                  </button>
                  <button v-if="order.status === 'completed'" class="action-btn success" @click="openCostModal(order)">
                    💰 Изменить стоимость
                  </button>
                </div>
              </div>
            </div>
            
            <!-- Пустой список -->
            <div v-if="!loading.orders && !error.orders && filteredOrders.length === 0" class="empty-state">
              <p>📭 Заказов не найдено</p>
              <button v-if="orderFilter.status || orderFilter.urgency" @click="clearFilters">
                Очистить фильтры
              </button>
              <button v-else @click="showNewOrderModal = true">
                Создать первый заказ
              </button>
            </div>
          </div>

          <!-- Вкладка Клиенты -->
          <div v-if="activeTab === 'clients'">
            <!-- Статистика клиентов -->
            <div class="client-stats">
              <div class="stat-card">
                <h3>👥 Всего клиентов</h3>
                <div class="stat-value">{{ clients.length }}</div>
              </div>
              <div class="stat-card">
                <h3>✅ Активные</h3>
                <div class="stat-value">{{ clients.filter(c => c.isActive).length }}</div>
              </div>
              <div class="stat-card">
                <h3>📋 Всего заказов</h3>
                <div class="stat-value">{{ totalClientOrders }}</div>
              </div>
            </div>
            
            <!-- Состояния -->
            <div v-if="loading.clients" class="loading-state">
              <div class="spinner"></div>
              <p>Загрузка клиентов...</p>
            </div>
            
            <div v-else-if="error.clients" class="error-state">
              <p>❌ Ошибка: {{ error.clients }}</p>
              <button @click="loadClients">Повторить</button>
            </div>
            
            <!-- Список клиентов -->
            <div v-else class="clients-grid">
              <div v-for="client in clients" :key="client.id" class="client-card">
                <div class="client-header">
                  <h3>{{ client.name }}</h3>
                  <span :class="['status-indicator', client.isActive ? 'active' : 'inactive']">
                    {{ client.isActive ? '✅ Активен' : '❌ Неактивен' }}
                  </span>
                </div>
                
                <div class="client-info">
                  <p><strong>📞 Телефон:</strong> {{ client.phone }}</p>
                  <p v-if="client.email"><strong>📧 Email:</strong> {{ client.email }}</p>
                  <p><strong>📅 Зарегистрирован:</strong> {{ formatDate(client.createdAt) }}</p>
                  <p><strong>📋 Заказов:</strong> {{ client.ordersCount || 0 }}</p>
                </div>
                
                <div class="client-actions">
                  <button class="action-btn primary" @click="editClient(client)">
                    ✏️ Редактировать
                  </button>
                  <button class="action-btn secondary" @click="createOrderForClient(client)">
                    📝 Новый заказ
                  </button>
                </div>
              </div>
            </div>
            
            <!-- Пустой список -->
            <div v-if="!loading.clients && !error.clients && clients.length === 0" class="empty-state">
              <p>👥 Клиентов нет</p>
              <button @click="showNewClientModal = true">
                Добавить первого клиента
              </button>
            </div>
          </div>

          <!-- Вкладка Мастера -->
          <div v-if="activeTab === 'masters'">
            <!-- Профиль текущего мастера -->
            <div v-if="currentMaster" class="master-profile">
              <div class="profile-header">
                <div class="master-avatar">
                  {{ getInitials(currentMaster.name) }}
                </div>
                <div class="profile-info">
                  <h3>{{ currentMaster.name }}</h3>
                  <p class="specialization">{{ currentMaster.specialization || 'Специализация не указана' }}</p>
                  <div class="profile-stats">
                    <span>⭐ {{ currentMaster.rating?.toFixed(1) || 'Нет оценок' }}</span>
                    <span>💰 {{ currentMaster.hourlyRate?.toLocaleString('ru-RU') }} ₽/час</span>
                    <span>📋 {{ currentMaster.ordersCount || 0 }} заказов</span>
                    <span>🔄 {{ currentMaster.currentOrders || 0 }} активных</span>
                  </div>
                </div>
              </div>
            </div>
            
            <!-- Список всех мастеров -->
            <div v-if="loading.masters" class="loading-state">
              <div class="spinner"></div>
              <p>Загрузка мастеров...</p>
            </div>
            
            <div v-else-if="error.masters" class="error-state">
              <p>❌ Ошибка: {{ error.masters }}</p>
              <button @click="loadMasters">Повторить</button>
            </div>
            
            <div v-else class="masters-grid">
              <div v-for="master in masters" :key="master.id" class="master-card">
                <div class="master-header">
                  <div class="master-avatar">
                    {{ getInitials(master.name) }}
                  </div>
                  <div class="master-title">
                    <h3>{{ master.name }}</h3>
                    <p class="specialization">{{ master.specialization || 'Специализация не указана' }}</p>
                  </div>
                  <div class="master-rating">
                    ⭐ {{ master.rating?.toFixed(1) || 'Нет оценок' }}
                  </div>
                </div>
                
                <div class="master-info">
                  <p v-if="master.email"><strong>📧 Email:</strong> {{ master.email }}</p>
                  <p v-if="master.phone"><strong>📞 Телефон:</strong> {{ master.phone }}</p>
                  <p><strong>💰 Ставка:</strong> {{ master.hourlyRate?.toLocaleString('ru-RU') }} ₽/час</p>
                  <p><strong>📊 Всего заказов:</strong> {{ master.ordersCount || 0 }}</p>
                  <p><strong>🔄 Текущие заказы:</strong> {{ master.currentOrders || 0 }}</p>
                  <p><strong>📅 В команде с:</strong> {{ formatDate(master.createdAt) }}</p>
                  <p><strong>📈 Статус:</strong> {{ master.isActive ? '✅ Активен' : '❌ Неактивен' }}</p>
                </div>
                
                <div class="master-actions">
                  
                </div>
              </div>
            </div>
          </div>

          <!-- Вкладка Запчасти -->
          <div v-if="activeTab === 'parts'">
            <!-- Статистика запчастей -->
            <div class="parts-stats">
              <div class="stat-card">
                <h3>🔩 Всего запчастей</h3>
                <div class="stat-value">{{ spareParts.length }}</div>
              </div>
              <div class="stat-card">
                <h3>📦 Общий запас</h3>
                <div class="stat-value">{{ totalPartsQuantity }} шт.</div>
              </div>
              <div class="stat-card">
                <h3>⚠️ Низкий запас</h3>
                <div class="stat-value">{{ lowStockParts.length }} шт.</div>
              </div>
            </div>
            
            <!-- Состояния -->
            <div v-if="loading.parts" class="loading-state">
              <div class="spinner"></div>
              <p>Загрузка запчастей...</p>
            </div>
            
            <div v-else-if="error.parts" class="error-state">
              <p>❌ Ошибка: {{ error.parts }}</p>
              <button @click="loadSpareParts">Повторить</button>
            </div>
            
            <div v-else class="parts-grid">
              <div v-for="part in spareParts" :key="part.id" class="part-card"
                   :class="{ 'low-stock': part.quantity <= part.minStockLevel }">
                <div class="part-header">
                  <h3>{{ part.name }}</h3>
                  <span class="sku">Арт: {{ part.sku }}</span>
                </div>
                
                <div class="part-info">
                  <p v-if="part.manufacturer"><strong>🏭 Производитель:</strong> {{ part.manufacturer }}</p>
                  <p v-if="part.description"><strong>📝 Описание:</strong> {{ truncateText(part.description, 80) }}</p>
                </div>
                
                <div class="part-stock">
                  <div class="stock-info">
                    <span :class="['quantity', part.quantity <= part.minStockLevel ? 'critical' : 'normal']">
                      📦 {{ part.quantity }} шт.
                    </span>
                    <span class="price">💰 {{ part.price.toLocaleString('ru-RU') }} ₽</span>
                  </div>
                  <div class="stock-min">
                    Мин. запас: {{ part.minStockLevel }} шт.
                  </div>
                </div>
                
                <div class="part-actions">
                  <button class="action-btn primary" @click="editPart(part)">
                    ✏️ Редактировать
                  </button>
                  <button class="action-btn secondary" @click="openUsePartModal(part)">
                    📝 Использовать
                  </button>
                </div>
              </div>
            </div>
            
            <!-- Пустой список -->
            <div v-if="!loading.parts && !error.parts && spareParts.length === 0" class="empty-state">
              <p>🔩 Запчастей нет</p>
              <button @click="showNewPartModal = true">
                Добавить первую запчасть
              </button>
            </div>
          </div>
        </div>
      </main>
    </div>

    <!-- Сообщение о необходимости входа -->
    <div v-else class="login-prompt">
      <div class="login-content">
        <h2>🔐 Вход в систему</h2>
        <p>Для работы с системой необходимо войти в аккаунт мастера</p>
        <button class="login-btn" @click="showLoginModal = true">Войти в систему</button>
      </div>
    </div>

    <!-- Модальные окна -->

    <!-- Новый заказ -->
    <div v-if="showNewOrderModal" class="modal-overlay">
      <div class="modal">
        <div class="modal-header">
          <h3>📝 Новый заказ</h3>
          <button class="close-btn" @click="closeNewOrderModal">×</button>
        </div>
        <div class="modal-content">
          <form @submit.prevent="createOrder">
            <div class="form-group">
              <label>👤 Клиент:</label>
              <select v-model="newOrder.clientId" required>
                <option value="">Выберите клиента</option>
                <option v-for="client in clients" :key="client.id" :value="client.id">
                  {{ client.name }} ({{ client.phone }})
                </option>
              </select>
            </div>
            
            <div class="form-row">
              <div class="form-group">
                <label>📱 Устройство:</label>
                <input v-model="newOrder.deviceName" type="text" required placeholder="Например: iPhone 12">
              </div>
              <div class="form-group">
                <label>🔢 Модель:</label>
                <input v-model="newOrder.deviceModel" type="text" placeholder="Модель">
              </div>
            </div>
            
            <div class="form-group">
              <label>🔢 Серийный номер:</label>
              <input v-model="newOrder.serialNumber" type="text" placeholder="SN123456">
            </div>
            
            <div class="form-group">
              <label>⚠️ Описание проблемы:</label>
              <textarea v-model="newOrder.problemDescription" required rows="3"
                        placeholder="Подробное описание неисправности..."></textarea>
            </div>
            
            <div class="form-row">
              <div class="form-group">
                <label>💰 Стоимость запчастей (₽):</label>
                <input v-model.number="newOrder.partsCost" type="number" min="0" step="0.01" value="0">
              </div>
              <div class="form-group">
                <label>⏱️ Стоимость работы (₽):</label>
                <input v-model.number="newOrder.laborCost" type="number" min="0" step="0.01" value="1000">
              </div>
            </div>
            
            <div class="form-row">
              <div class="form-group">
                <label>⚡ Приоритет:</label>
                <div class="checkbox-group">
                  <label>
                    <input type="checkbox" v-model="newOrder.isUrgent">
                    Срочный заказ
                  </label>
                </div>
              </div>
              <div class="form-group">
                <label>📅 Гарантия (дней):</label>
                <input v-model.number="newOrder.warrantyPeriod" type="number" min="0" value="90">
              </div>
            </div>
            
            <div class="form-group">
              <label>💰 Итоговая стоимость:</label>
              <div class="cost-preview">
                <div>Запчасти: {{ newOrder.partsCost?.toLocaleString('ru-RU') || 0 }} ₽</div>
                <div>Работа: {{ newOrder.laborCost?.toLocaleString('ru-RU') || 0 }} ₽</div>
                <div v-if="newOrder.isUrgent">Срочность: +1000 ₽</div>
                <div class="total-cost">
                  <strong>Итого: {{ calculateTotalCost() }} ₽</strong>
                </div>
              </div>
            </div>
            
            <div class="form-actions">
              <button type="button" class="cancel-btn" @click="closeNewOrderModal">
                Отмена
              </button>
              <button type="submit" class="submit-btn" :disabled="creatingOrder">
                {{ creatingOrder ? 'Создание...' : 'Создать заказ' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Новый клиент -->
    <div v-if="showNewClientModal" class="modal-overlay">
      <div class="modal">
        <div class="modal-header">
          <h3>👤 Новый клиент</h3>
          <button class="close-btn" @click="closeNewClientModal">×</button>
        </div>
        <div class="modal-content">
          <form @submit.prevent="createClient">
            <div class="form-group">
              <label>👤 ФИО:</label>
              <input v-model="newClient.name" type="text" required placeholder="Иван Иванов">
            </div>
            
            <div class="form-group">
              <label>📞 Телефон:</label>
              <input v-model="newClient.phone" type="tel" required placeholder="+7 (999) 123-45-67">
            </div>
            
            <div class="form-group">
              <label>📧 Email:</label>
              <input v-model="newClient.email" type="email" placeholder="client@example.com">
            </div>
            
            <div class="form-actions">
              <button type="button" class="cancel-btn" @click="closeNewClientModal">
                Отмена
              </button>
              <button type="submit" class="submit-btn" :disabled="creatingClient">
                {{ creatingClient ? 'Создание...' : 'Создать клиента' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Новая запчасть -->
    <div v-if="showNewPartModal" class="modal-overlay">
      <div class="modal">
        <div class="modal-header">
          <h3>🔩 Новая запчасть</h3>
          <button class="close-btn" @click="closeNewPartModal">×</button>
        </div>
        <div class="modal-content">
          <form @submit.prevent="createPart">
            <div class="form-row">
              <div class="form-group">
                <label>📝 Название:</label>
                <input v-model="newPart.name" type="text" required placeholder="Аккумулятор iPhone 13">
              </div>
              <div class="form-group">
                <label>🏷️ Артикул:</label>
                <input v-model="newPart.sku" type="text" required placeholder="BATT-IP13-001">
              </div>
            </div>
            
            <div class="form-group">
              <label>🏭 Производитель:</label>
              <input v-model="newPart.manufacturer" type="text" placeholder="Apple">
            </div>
            
            <div class="form-group">
              <label>📋 Описание:</label>
              <textarea v-model="newPart.description" rows="3" placeholder="Описание запчасти..."></textarea>
            </div>
            
            <div class="form-row">
              <div class="form-group">
                <label>📦 Количество:</label>
                <input v-model.number="newPart.quantity" type="number" min="0" required value="10">
              </div>
              <div class="form-group">
                <label>💰 Цена (₽):</label>
                <input v-model.number="newPart.price" type="number" min="0" step="0.01" required value="1000">
              </div>
              <div class="form-group">
                <label>⚠️ Мин. запас:</label>
                <input v-model.number="newPart.minStockLevel" type="number" min="1" value="5">
              </div>
            </div>
            
            <div class="form-actions">
              <button type="button" class="cancel-btn" @click="closeNewPartModal">
                Отмена
              </button>
              <button type="submit" class="submit-btn" :disabled="creatingPart">
                {{ creatingPart ? 'Создание...' : 'Создать запчасть' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Редактирование клиента -->
    <div v-if="showEditClientModal" class="modal-overlay">
      <div class="modal">
        <div class="modal-header">
          <h3>✏️ Редактирование клиента</h3>
          <button class="close-btn" @click="showEditClientModal = false">×</button>
        </div>
        <div class="modal-content">
          <form @submit.prevent="updateClient">
            <div class="form-group">
              <label>👤 ФИО:</label>
              <input v-model="editingClient.name" type="text" required>
            </div>
            
            <div class="form-group">
              <label>📞 Телефон:</label>
              <input v-model="editingClient.phone" type="tel" required>
            </div>
            
            <div class="form-group">
              <label>📧 Email:</label>
              <input v-model="editingClient.email" type="email">
            </div>
            
            <div class="form-group">
              <label>📅 Статус:</label>
              <div class="checkbox-group">
                <label>
                  <input type="checkbox" v-model="editingClient.isActive">
                  Активный клиент
                </label>
              </div>
            </div>
            
            <div class="form-actions">
              <button type="button" class="cancel-btn" @click="showEditClientModal = false">
                Отмена
              </button>
              <button type="submit" class="submit-btn">
                Сохранить
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Редактирование запчасти -->
    <div v-if="showEditPartModal" class="modal-overlay">
      <div class="modal">
        <div class="modal-header">
          <h3>✏️ Редактирование запчасти</h3>
          <button class="close-btn" @click="showEditPartModal = false">×</button>
        </div>
        <div class="modal-content">
          <form @submit.prevent="updatePart">
            <div class="form-row">
              <div class="form-group">
                <label>📝 Название:</label>
                <input v-model="editingPart.name" type="text" required>
              </div>
              <div class="form-group">
                <label>🏷️ Артикул:</label>
                <input v-model="editingPart.sku" type="text" required>
              </div>
            </div>
            
            <div class="form-group">
              <label>🏭 Производитель:</label>
              <input v-model="editingPart.manufacturer" type="text">
            </div>
            
            <div class="form-group">
              <label>📋 Описание:</label>
              <textarea v-model="editingPart.description" rows="3"></textarea>
            </div>
            
            <div class="form-row">
              <div class="form-group">
                <label>📦 Количество:</label>
                <input v-model.number="editingPart.quantity" type="number" min="0" required>
              </div>
              <div class="form-group">
                <label>💰 Цена (₽):</label>
                <input v-model.number="editingPart.price" type="number" min="0" step="0.01" required>
              </div>
              <div class="form-group">
                <label>⚠️ Мин. запас:</label>
                <input v-model.number="editingPart.minStockLevel" type="number" min="1">
              </div>
            </div>
            
            <div class="form-actions">
              <button type="button" class="cancel-btn" @click="showEditPartModal = false">
                Отмена
              </button>
              <button type="submit" class="submit-btn">
                Сохранить
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Использование запчасти -->
    <div v-if="showUsePartModal" class="modal-overlay">
      <div class="modal">
        <div class="modal-header">
          <h3>📝 Использование запчасти</h3>
          <button class="close-btn" @click="showUsePartModal = false">×</button>
        </div>
        <div class="modal-content">
          <form @submit.prevent="usePart">
            <div class="form-group">
              <label>Запчасть:</label>
              <input type="text" :value="selectedPart?.name" disabled>
            </div>
            
            <div class="form-group">
              <label>📦 Доступно:</label>
              <input type="text" :value="`${selectedPart?.quantity} шт.`" disabled>
            </div>
            
            <div class="form-group">
              <label>📝 Количество для использования:</label>
              <input v-model.number="usePartData.quantity" type="number" min="1" :max="selectedPart?.quantity" required value="1">
            </div>
            
            <div class="form-group">
              <label>💰 Общая стоимость:</label>
              <div class="cost-preview">
                {{ usePartData.quantity }} × {{ selectedPart?.price.toLocaleString('ru-RU') }} ₽ = {{ (usePartData.quantity * (selectedPart?.price || 0)).toLocaleString('ru-RU') }} ₽
              </div>
            </div>
            
            <div class="form-actions">
              <button type="button" class="cancel-btn" @click="showUsePartModal = false">
                Отмена
              </button>
              <button type="submit" class="submit-btn">
                Списать запчасть
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Изменение статуса заказа -->
    <div v-if="showStatusModal" class="modal-overlay">
      <div class="modal">
        <div class="modal-header">
          <h3>📝 Изменение статуса заказа</h3>
          <button class="close-btn" @click="showStatusModal = false">×</button>
        </div>
        <div class="modal-content">
          <form @submit.prevent="updateOrderStatus">
            <div class="form-group">
              <label>Новый статус:</label>
              <select v-model="statusUpdate.status" required>
                <option value="new">🆕 Новый</option>
                <option value="accepted">✅ Принят</option>
                <option value="in_progress">🔧 В работе</option>
                <option value="waiting_parts">⏳ Ожидание запчастей</option>
                <option value="repair">🔧 Ремонт</option>
                <option value="ready">📦 Готов к выдаче</option>
                <option value="completed">🏁 Завершен</option>
                <option value="cancelled">❌ Отменен</option>
              </select>
            </div>
            
            <div class="form-group">
              <label>📝 Диагностические заметки:</label>
              <textarea v-model="statusUpdate.diagnosticNotes" rows="3"></textarea>
            </div>
            
            <div class="form-actions">
              <button type="button" class="cancel-btn" @click="showStatusModal = false">
                Отмена
              </button>
              <button type="submit" class="submit-btn">
                Обновить статус
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Изменение стоимости заказа -->
    <div v-if="showCostModal" class="modal-overlay">
      <div class="modal">
        <div class="modal-header">
          <h3>💰 Изменение стоимости заказа</h3>
          <button class="close-btn" @click="showCostModal = false">×</button>
        </div>
        <div class="modal-content">
          <form @submit.prevent="updateOrderCost">
            <div class="form-row">
              <div class="form-group">
                <label>🛠️ Стоимость запчастей (₽):</label>
                <input v-model.number="costUpdate.partsCost" type="number" min="0" step="0.01" required>
              </div>
              <div class="form-group">
                <label>⏱️ Стоимость работы (₽):</label>
                <input v-model.number="costUpdate.laborCost" type="number" min="0" step="0.01" required>
              </div>
            </div>
            
            <div class="form-group">
              <label>💰 Итоговая стоимость:</label>
              <div class="cost-preview">
                {{ costUpdate.partsCost.toLocaleString('ru-RU') }} + {{ costUpdate.laborCost.toLocaleString('ru-RU') }} = <strong>{{ (costUpdate.partsCost + costUpdate.laborCost).toLocaleString('ru-RU') }} ₽</strong>
              </div>
            </div>
            
            <div class="form-actions">
              <button type="button" class="cancel-btn" @click="showCostModal = false">
                Отмена
              </button>
              <button type="submit" class="submit-btn">
                Обновить стоимость
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Детали заказа -->
    <div v-if="showOrderDetailsModal" class="modal-overlay">
      <div class="modal large-modal">
        <div class="modal-header">
          <h3>👁️ Детали заказа #{{ selectedOrder?.orderNumber }}</h3>
          <button class="close-btn" @click="showOrderDetailsModal = false">×</button>
        </div>
        <div class="modal-content">
          <div v-if="selectedOrder" class="order-details">
            <div class="detail-section">
              <h4>📱 Информация об устройстве</h4>
              <div class="detail-row">
                <span class="label">Устройство:</span>
                <span>{{ selectedOrder.device }}</span>
              </div>
              <div v-if="selectedOrder.deviceModel" class="detail-row">
                <span class="label">Модель:</span>
                <span>{{ selectedOrder.deviceModel }}</span>
              </div>
              <div v-if="selectedOrder.serialNumber" class="detail-row">
                <span class="label">Серийный номер:</span>
                <span>{{ selectedOrder.serialNumber }}</span>
              </div>
              <div class="detail-row">
                <span class="label">Проблема:</span>
                <span>{{ selectedOrder.problemDescription }}</span>
              </div>
            </div>
            
            <div class="detail-section">
              <h4>👤 Информация о клиенте</h4>
              <div class="detail-row">
                <span class="label">Имя:</span>
                <span>{{ selectedOrder.clientName }}</span>
              </div>
              <div v-if="selectedOrder.clientPhone" class="detail-row">
                <span class="label">Телефон:</span>
                <span>{{ selectedOrder.clientPhone }}</span>
              </div>
            </div>
            
            <div class="detail-section">
              <h4>🛠️ Информация о ремонте</h4>
              <div class="detail-row">
                <span class="label">Статус:</span>
                <span :class="['status-badge', getStatusClass(selectedOrder.status)]">
                  {{ getStatusText(selectedOrder.status) }}
                </span>
              </div>
              <div class="detail-row">
                <span class="label">Мастер:</span>
                <span>{{ selectedOrder.masterName || 'Не назначен' }}</span>
              </div>
              <div class="detail-row">
                <span class="label">Принят:</span>
                <span>{{ formatDate(selectedOrder.createdDate) }}</span>
              </div>
              <div v-if="selectedOrder.acceptedAt" class="detail-row">
                <span class="label">Начат:</span>
                <span>{{ formatDate(selectedOrder.acceptedAt) }}</span>
              </div>
              <div v-if="selectedOrder.completedAt" class="detail-row">
                <span class="label">Завершен:</span>
                <span>{{ formatDate(selectedOrder.completedAt) }}</span>
              </div>
              <div class="detail-row">
                <span class="label">Стоимость запчастей:</span>
                <span>{{ selectedOrder.partsCost?.toLocaleString('ru-RU') || 0 }} ₽</span>
              </div>
              <div class="detail-row">
                <span class="label">Стоимость работы:</span>
                <span>{{ selectedOrder.laborCost?.toLocaleString('ru-RU') || 0 }} ₽</span>
              </div>
              <div class="detail-row">
                <span class="label">Общая сумма:</span>
                <span class="price">{{ selectedOrder.totalAmount?.toLocaleString('ru-RU') || 0 }} ₽</span>
              </div>
              <div v-if="selectedOrder.estimatedCompletionDate" class="detail-row">
                <span class="label">Завершение:</span>
                <span>{{ formatDate(selectedOrder.estimatedCompletionDate) }}</span>
              </div>
              <div v-if="selectedOrder.diagnosticNotes" class="detail-row">
                <span class="label">Диагностика:</span>
                <span>{{ selectedOrder.diagnosticNotes }}</span>
              </div>
              <div class="detail-row">
                <span class="label">Гарантия:</span>
                <span>{{ selectedOrder.warrantyPeriod || 90 }} дней</span>
              </div>
              <div class="detail-row">
                <span class="label">Срочный:</span>
                <span>{{ selectedOrder.isUrgent ? 'Да' : 'Нет' }}</span>
              </div>
            </div>
            
            <div class="modal-actions">
              <button class="action-btn primary" @click="openStatusModal(selectedOrder)">
                📝 Изменить статус
              </button>
              <button class="action-btn secondary" @click="openCostModal(selectedOrder)" 
                      v-if="selectedOrder.status === 'completed'">
                💰 Изменить стоимость
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, computed } from 'vue'
import api from './services/api.js'

export default {
  setup() {
    // Состояние приложения
    const activeTab = ref('orders')
    
    // Модальные окна
    const showLoginModal = ref(false)
    const showNewOrderModal = ref(false)
    const showNewClientModal = ref(false)
    const showNewPartModal = ref(false)
    const showEditClientModal = ref(false)
    const showEditPartModal = ref(false)
    const showUsePartModal = ref(false)
    const showStatusModal = ref(false)
    const showCostModal = ref(false)
    const showOrderDetailsModal = ref(false)
    
    // Данные
    const orders = ref([])
    const clients = ref([])
    const masters = ref([])
    const spareParts = ref([])
    const currentMaster = ref(null)
    const selectedOrder = ref(null)
    const selectedPart = ref(null)
    const editingClient = ref(null)
    const editingPart = ref(null)
    
    // Состояния загрузки
    const loading = ref({
      orders: false,
      clients: false,
      masters: false,
      parts: false
    })
    
    // Ошибки
    const error = ref({
      orders: '',
      clients: '',
      masters: '',
      parts: ''
    })
    
    // Флаги операций
    const loggingIn = ref(false)
    const creatingOrder = ref(false)
    const creatingClient = ref(false)
    const creatingPart = ref(false)
    
    // Фильтры
    const orderFilter = ref({
      status: '',
      urgency: '',
      masterId: ''
    })
    
    // Формы
    const loginForm = ref({
      masterId: '',
      username: '',
      password: ''
    })
    
    const newOrder = ref({
      clientId: '',
      deviceName: '',
      deviceModel: '',
      serialNumber: '',
      problemDescription: '',
      masterId: null,
      partsCost: 0,
      laborCost: 1000,
      isUrgent: false,
      warrantyPeriod: 90
    })
    
    const newClient = ref({
      name: '',
      phone: '',
      email: '',
      isActive: true
    })
    
    const newPart = ref({
      name: '',
      sku: '',
      manufacturer: '',
      description: '',
      quantity: 10,
      price: 1000,
      minStockLevel: 5
    })
    
    const statusUpdate = ref({
      orderId: null,
      status: '',
      diagnosticNotes: '',
      masterId: '',
      estimatedCompletionDate: ''
    })
    
    const costUpdate = ref({
      orderId: null,
      partsCost: 0,
      laborCost: 0
    })
    
    const usePartData = ref({
      partId: null,
      quantity: 1,
      orderId: ''
    })
    
    // Вкладки
    const tabs = ref([
      { id: 'orders', name: '📋 Заказы' },
      { id: 'clients', name: '👥 Клиенты' },
      { id: 'masters', name: '👨‍🔧 Мастера' },
      { id: 'parts', name: '🔧 Запчасти' }
    ])
    
    // Вычисляемые свойства
    const filteredOrders = computed(() => {
      let filtered = orders.value
      
      if (orderFilter.value.status) {
        filtered = filtered.filter(o => o.status === orderFilter.value.status)
      }
      
      if (orderFilter.value.urgency === 'urgent') {
        filtered = filtered.filter(o => o.isUrgent)
      }
      
      return filtered
    })
    
    const totalOrdersAmount = computed(() => {
      return orders.value.reduce((sum, order) => sum + (order.totalAmount || 0), 0)
    })
    
    const totalClientOrders = computed(() => {
      return clients.value.reduce((sum, client) => sum + (client.ordersCount || 0), 0)
    })
    
    const lowStockParts = computed(() => {
      return spareParts.value.filter(part => part.quantity <= part.minStockLevel)
    })
    
    const totalPartsQuantity = computed(() => {
      return spareParts.value.reduce((sum, part) => sum + part.quantity, 0)
    })
    
    const availableMasters = computed(() => {
      return masters.value.filter(m => m.isActive)
    })
    
    // Методы
    function calculateLaborCost() {
      if (!currentMaster.value) return 1000
      // Стоимость работы = ставка мастера * 1.5 часа
      return Math.round(currentMaster.value.hourlyRate * 1.5)
    }
    
    function calculateTotalCost() {
      let total = (newOrder.value.partsCost || 0) + (newOrder.value.laborCost || calculateLaborCost())
      if (newOrder.value.isUrgent) {
        total += 1000
      }
      return total.toLocaleString('ru-RU')
    }
    
    // Функция для получения текста статуса
    function getStatusText(status) {
      const statusMap = {
        'new': '🆕 Новый',
        'accepted': '✅ Принят',
        'in_progress': '🔧 В работе',
        'waiting_parts': '⏳ Ожидание запчастей',
        'repair': '🔧 Ремонт',
        'ready': '📦 Готов к выдаче',
        'completed': '🏁 Завершен',
        'cancelled': '❌ Отменен'
      }
      return statusMap[status] || status
    }
    
    // Функция для получения класса статуса
    function getStatusClass(status) {
      return status.replace(/_/g, '-')
    }
    
    async function login() {
      console.log('Начинаем вход...', loginForm.value)
      
      if (!loginForm.value.masterId) {
        alert('Пожалуйста, выберите мастера')
        return
      }
      
      loggingIn.value = true
      
      try {
        // Находим выбранного мастера
        const selectedMaster = masters.value.find(m => m.id == loginForm.value.masterId)
        console.log('Выбранный мастер:', selectedMaster)
        
        if (!selectedMaster) {
          throw new Error('Выбранный мастер не найден в списке')
        }
        
        // Используем демо-авторизацию
        const demoResponse = {
          success: true,
          masterId: selectedMaster.id,
          masterName: selectedMaster.name,
          token: `demo-token-${selectedMaster.id}-${Date.now()}`,
          master: {
            id: selectedMaster.id,
            name: selectedMaster.name,
            specialization: selectedMaster.specialization,
            email: selectedMaster.email,
            phone: selectedMaster.phone,
            hourlyRate: selectedMaster.hourlyRate,
            rating: selectedMaster.rating,
            isActive: selectedMaster.isActive,
            createdAt: selectedMaster.createdAt,
            ordersCount: selectedMaster.ordersCount || 0,
            currentOrders: selectedMaster.currentOrders || 0
          }
        }
        
        // Сохраняем данные мастера
        currentMaster.value = demoResponse.master
        localStorage.setItem('masterToken', demoResponse.token)
        localStorage.setItem('master', JSON.stringify(demoResponse.master))
        
        // Закрываем окно входа
        showLoginModal.value = false
        
        // Сбрасываем форму
        loginForm.value = { masterId: '', username: '', password: '' }
        
        // Загружаем данные
        await loadData()
        
        console.log('Вход выполнен успешно:', currentMaster.value.name)
        
      } catch (err) {
        console.error('Login error:', err)
        alert('Ошибка входа: ' + err.message)
        
        // Пробуем использовать последнего доступного мастера
        if (masters.value.length > 0) {
          const fallbackMaster = masters.value[0]
          currentMaster.value = {
            id: fallbackMaster.id,
            name: fallbackMaster.name,
            specialization: fallbackMaster.specialization,
            email: fallbackMaster.email,
            phone: fallbackMaster.phone,
            hourlyRate: fallbackMaster.hourlyRate,
            rating: fallbackMaster.rating,
            isActive: fallbackMaster.isActive,
            createdAt: fallbackMaster.createdAt,
            ordersCount: fallbackMaster.ordersCount || 0,
            currentOrders: fallbackMaster.currentOrders || 0
          }
          localStorage.setItem('master', JSON.stringify(currentMaster.value))
          localStorage.setItem('masterToken', `demo-token-${fallbackMaster.id}-${Date.now()}`)
          showLoginModal.value = false
          await loadData()
        }
      } finally {
        loggingIn.value = false
      }
    }
    
    async function logout() {
      if (confirm('Вы уверены, что хотите выйти?')) {
        try {
          await api.logout()
        } catch (err) {
          console.error('Logout error:', err)
        } finally {
          currentMaster.value = null
          localStorage.removeItem('masterToken')
          localStorage.removeItem('master')
          // Показываем окно входа
          showLoginModal.value = true
          // Сбрасываем форму
          loginForm.value = { masterId: '', username: '', password: '' }
          console.log('Выход выполнен')
        }
      }
    }
    
    async function loadData() {
      if (!currentMaster.value) return
      
      try {
        await loadMasters() // Обновляем список мастеров
        await loadClients()
        await loadOrders()
        await loadSpareParts()
      } catch (err) {
        console.error('Error loading data:', err)
      }
    }
    
    async function loadOrders() {
      loading.value.orders = true
      error.value.orders = ''
      try {
        const response = await api.getOrders()
        orders.value = response.data || []
      } catch (err) {
        error.value.orders = err.response?.data?.error || err.message || 'Ошибка загрузки'
        console.error('Error loading orders:', err)
      } finally {
        loading.value.orders = false
      }
    }
    
    async function loadClients() {
      loading.value.clients = true
      try {
        const response = await api.getClients()
        clients.value = response.data || []
      } catch (err) {
        console.error('Error loading clients:', err)
      } finally {
        loading.value.clients = false
      }
    }
    
    async function loadMasters() {
      loading.value.masters = true
      try {
        const response = await api.getMasters()
        masters.value = response.data || []
        
        // Если нет мастеров в базе, создаем демо-мастеров
        if (masters.value.length === 0) {
          const demoMasters = [
            {
              id: 1,
              name: 'Петр Васильев',
              specialization: 'Смартфоны, планшеты',
              email: 'petr@servicedesk.ru',
              phone: '+7 (999) 111-22-33',
              hourlyRate: 850,
              rating: 4.8,
              isActive: true,
              createdAt: new Date().toISOString(),
              ordersCount: 0,
              currentOrders: 0
            },
            {
              id: 2,
              name: 'Сергей Козлов',
              specialization: 'Ноутбуки, компьютеры',
              email: 'sergey@servicedesk.ru',
              phone: '+7 (999) 222-33-44',
              hourlyRate: 950,
              rating: 4.9,
              isActive: true,
              createdAt: new Date().toISOString(),
              ordersCount: 0,
              currentOrders: 0
            }
          ]
          masters.value = demoMasters
        }
        
        // Автоматически выбираем первого мастера в форме входа
        if (showLoginModal.value && masters.value.length > 0 && !loginForm.value.masterId) {
          loginForm.value.masterId = masters.value[0].id
        }
        
      } catch (err) {
        console.error('Error loading masters:', err)
        // Создаем демо-мастеров при ошибке
        masters.value = [
          {
            id: 1,
            name: 'Демо Мастер',
            specialization: 'Демо специализация',
            email: 'demo@servicedesk.ru',
            phone: '+7 (999) 000-00-00',
            hourlyRate: 1000,
            rating: 4.5,
            isActive: true,
            createdAt: new Date().toISOString(),
            ordersCount: 0,
            currentOrders: 0
          }
        ]
      } finally {
        loading.value.masters = false
      }
    }
    
    async function loadSpareParts() {
      loading.value.parts = true
      try {
        const response = await api.getSpareParts()
        spareParts.value = response.data || []
      } catch (err) {
        console.error('Error loading parts:', err)
      } finally {
        loading.value.parts = false
      }
    }
    
    async function createOrder() {
      if (!newOrder.value.clientId) {
        alert('Пожалуйста, выберите клиента')
        return
      }
      
      if (!currentMaster.value) {
        alert('Ошибка: мастер не определен')
        return
      }
      
      creatingOrder.value = true
      try {
        const orderData = {
          clientId: parseInt(newOrder.value.clientId),
          deviceName: newOrder.value.deviceName,
          deviceModel: newOrder.value.deviceModel || '',
          serialNumber: newOrder.value.serialNumber || '',
          problemDescription: newOrder.value.problemDescription,
          masterId: currentMaster.value.id,
          isUrgent: newOrder.value.isUrgent,
          warrantyPeriod: newOrder.value.warrantyPeriod || 90
        }
        
        const response = await api.createOrder(orderData)
        
        // После создания заказа обновляем его стоимость
        if (response.data.id) {
          const costData = {
            partsCost: newOrder.value.partsCost || 0,
            laborCost: newOrder.value.laborCost || calculateLaborCost()
          }
          
          const costResponse = await api.updateOrderCost(response.data.id, costData)
          orders.value.unshift(costResponse.data)
        } else {
          orders.value.unshift(response.data)
        }
        
        closeNewOrderModal()
        alert('✅ Заказ успешно создан!')
      } catch (err) {
        console.error('Error creating order:', err)
        alert('❌ Ошибка создания заказа: ' + (err.response?.data?.error || err.message))
      } finally {
        creatingOrder.value = false
      }
    }
    
    async function createClient() {
      if (!newClient.value.name.trim()) {
        alert('Пожалуйста, введите ФИО клиента')
        return
      }
      
      if (!newClient.value.phone.trim()) {
        alert('Пожалуйста, введите телефон клиента')
        return
      }
      
      creatingClient.value = true
      try {
        const clientData = {
          name: newClient.value.name,
          phone: newClient.value.phone,
          email: newClient.value.email || '',
          isActive: true
        }
        
        const response = await api.createClient(clientData)
        clients.value.push(response.data)
        closeNewClientModal()
        alert('✅ Клиент успешно создан!')
      } catch (err) {
        console.error('Error creating client:', err)
        alert('❌ Ошибка создания клиента: ' + (err.response?.data?.error || err.message))
      } finally {
        creatingClient.value = false
      }
    }
    
    async function createPart() {
      if (!newPart.value.name.trim()) {
        alert('Пожалуйста, введите название запчасти')
        return
      }
      
      if (newPart.value.quantity < 0) {
        alert('Количество не может быть отрицательным')
        return
      }
      
      if (newPart.value.price <= 0) {
        alert('Цена должна быть больше 0')
        return
      }
      
      creatingPart.value = true
      try {
        const partData = {
          name: newPart.value.name,
          sku: newPart.value.sku || 'DEMO-' + Date.now(),
          manufacturer: newPart.value.manufacturer || '',
          description: newPart.value.description || '',
          quantity: newPart.value.quantity,
          price: newPart.value.price,
          minStockLevel: newPart.value.minStockLevel
        }
        
        const response = await api.createSparePart(partData)
        spareParts.value.push(response.data)
        closeNewPartModal()
        alert('✅ Запчасть успешно создана!')
      } catch (err) {
        console.error('Error creating part:', err)
        alert('❌ Ошибка создания запчасти: ' + (err.response?.data?.error || err.message))
      } finally {
        creatingPart.value = false
      }
    }
    
    function editClient(client) {
      editingClient.value = { ...client }
      showEditClientModal.value = true
    }
    
    async function updateClient() {
      if (!editingClient.value) return
      
      try {
        const clientData = {
          name: editingClient.value.name,
          phone: editingClient.value.phone,
          email: editingClient.value.email || '',
          isActive: editingClient.value.isActive
        }
        
        const response = await api.updateClient(editingClient.value.id, clientData)
        
        const index = clients.value.findIndex(c => c.id === editingClient.value.id)
        if (index !== -1) {
          clients.value[index] = response.data
        }
        
        showEditClientModal.value = false
        alert('✅ Клиент успешно обновлен!')
      } catch (err) {
        console.error('Error updating client:', err)
        alert('❌ Ошибка обновления клиента: ' + (err.response?.data?.error || err.message))
      }
    }
    
    function editPart(part) {
      editingPart.value = { ...part }
      showEditPartModal.value = true
    }
    
    async function updatePart() {
      if (!editingPart.value) return
      
      try {
        const partData = {
          name: editingPart.value.name,
          sku: editingPart.value.sku || 'DEMO-' + Date.now(),
          manufacturer: editingPart.value.manufacturer || '',
          description: editingPart.value.description || '',
          quantity: editingPart.value.quantity,
          price: editingPart.value.price,
          minStockLevel: editingPart.value.minStockLevel || 5
        }
        
        const response = await api.updateSparePart(editingPart.value.id, partData)
        
        const index = spareParts.value.findIndex(p => p.id === editingPart.value.id)
        if (index !== -1) {
          spareParts.value[index] = response.data
        }
        
        showEditPartModal.value = false
        alert('✅ Запчасть успешно обновлена!')
      } catch (err) {
        console.error('Error updating part:', err)
        alert('❌ Ошибка обновления запчасти: ' + (err.response?.data?.error || err.message))
      }
    }
    
    function openUsePartModal(part) {
      selectedPart.value = part
      usePartData.value = {
        partId: part.id,
        quantity: 1,
        orderId: ''
      }
      showUsePartModal.value = true
    }
    
    async function usePart() {
      if (!selectedPart.value) return
      
      try {
        const response = await api.useSparePart(selectedPart.value.id, usePartData.value.quantity)
        
        const index = spareParts.value.findIndex(p => p.id === selectedPart.value.id)
        if (index !== -1) {
          spareParts.value[index] = response.data
        }
        
        showUsePartModal.value = false
        alert(`✅ Использовано ${usePartData.value.quantity} шт. запчасти ${selectedPart.value.name}`)
      } catch (err) {
        console.error('Error using part:', err)
        alert('❌ Ошибка при использовании запчасти: ' + (err.response?.data?.error || err.message))
      }
    }
    
    async function viewOrder(id) {
      try {
        const response = await api.getOrder(id)
        selectedOrder.value = response.data
        showOrderDetailsModal.value = true
      } catch (err) {
        console.error('Error viewing order:', err)
        alert('Ошибка при загрузке деталей заказа: ' + (err.response?.data?.error || err.message))
      }
    }
    
    function openStatusModal(order) {
      selectedOrder.value = order
      statusUpdate.value = {
        orderId: order.id,
        status: order.status,
        diagnosticNotes: order.diagnosticNotes || '',
        masterId: '',
        estimatedCompletionDate: ''
      }
      showStatusModal.value = true
    }
    
    async function updateOrderStatus() {
      if (!statusUpdate.value.orderId) return
      
      try {
        const response = await api.updateOrderStatus(statusUpdate.value.orderId, {
          status: statusUpdate.value.status,
          diagnosticNotes: statusUpdate.value.diagnosticNotes || '',
          masterId: null,
          estimatedCompletionDate: null
        })
        
        const index = orders.value.findIndex(o => o.id === statusUpdate.value.orderId)
        if (index !== -1) {
          orders.value[index] = response.data
        }
        
        if (selectedOrder.value && selectedOrder.value.id === statusUpdate.value.orderId) {
          selectedOrder.value = response.data
        }
        
        showStatusModal.value = false
        alert('✅ Статус заказа обновлен!')
      } catch (err) {
        console.error('Error updating status:', err)
        alert('❌ Ошибка при обновлении статуса: ' + (err.response?.data?.error || err.message))
      }
    }
    
    function openCostModal(order) {
      selectedOrder.value = order
      costUpdate.value = {
        orderId: order.id,
        partsCost: order.partsCost || 0,
        laborCost: order.laborCost || 0
      }
      showCostModal.value = true
    }
    
    async function updateOrderCost() {
      if (!costUpdate.value.orderId) return
      
      try {
        const response = await api.updateOrderCost(costUpdate.value.orderId, {
          partsCost: costUpdate.value.partsCost,
          laborCost: costUpdate.value.laborCost
        })
        
        const index = orders.value.findIndex(o => o.id === costUpdate.value.orderId)
        if (index !== -1) {
          orders.value[index] = response.data
        }
        
        if (selectedOrder.value && selectedOrder.value.id === costUpdate.value.orderId) {
          selectedOrder.value = response.data
        }
        
        showCostModal.value = false
        alert('✅ Стоимость заказа обновлена!')
      } catch (err) {
        console.error('Error updating cost:', err)
        alert('❌ Ошибка при обновлении стоимости: ' + (err.response?.data?.error || err.message))
      }
    }
    
    function createOrderForClient(client) {
      activeTab.value = 'orders'
      newOrder.value.clientId = client.id.toString()
      showNewOrderModal.value = true
    }
    
    function assignOrderToMaster(master) {
      activeTab.value = 'orders'
      newOrder.value.masterId = master.id
      showNewOrderModal.value = true
    }
    
    function filterOrders() {
      // Автоматически обновляется через computed
    }
    
    function clearFilters() {
      orderFilter.value.status = ''
      orderFilter.value.urgency = ''
      orderFilter.value.masterId = ''
    }
    
    function closeNewOrderModal() {
      showNewOrderModal.value = false
      newOrder.value = {
        clientId: '',
        deviceName: '',
        deviceModel: '',
        serialNumber: '',
        problemDescription: '',
        masterId: null,
        partsCost: 0,
        laborCost: 1000,
        isUrgent: false,
        warrantyPeriod: 90
      }
    }
    
    function closeNewClientModal() {
      showNewClientModal.value = false
      newClient.value = {
        name: '',
        phone: '',
        email: '',
        isActive: true
      }
    }
    
    function closeNewPartModal() {
      showNewPartModal.value = false
      newPart.value = {
        name: '',
        sku: '',
        manufacturer: '',
        description: '',
        quantity: 10,
        price: 1000,
        minStockLevel: 5
      }
    }
    
    // Вспомогательные функции
    function switchTab(tabId) {
      activeTab.value = tabId
    }
    
    function getActiveTabName() {
      const tab = tabs.value.find(t => t.id === activeTab.value)
      return tab ? tab.name.replace(/[^а-яё\s]/gi, '') : ''
    }
    
    function formatDate(dateString) {
      if (!dateString) return 'Не указана'
      try {
        const date = new Date(dateString)
        return date.toLocaleDateString('ru-RU', {
          day: '2-digit',
          month: '2-digit',
          year: 'numeric',
          hour: '2-digit',
          minute: '2-digit'
        })
      } catch {
        return dateString
      }
    }
    
    function truncateText(text, maxLength) {
      if (!text) return ''
      if (text.length <= maxLength) return text
      return text.substring(0, maxLength) + '...'
    }
    
    function getInitials(name) {
      if (!name) return '?'
      return name.split(' ').map(n => n[0]).join('').toUpperCase()
    }
    
    // Инициализация
    onMounted(async () => {
      console.log('Инициализация приложения...')
      
      // Проверяем сохраненного мастера
      const savedMaster = localStorage.getItem('master')
      const savedToken = localStorage.getItem('masterToken')
      
      if (savedMaster && savedToken) {
        try {
          currentMaster.value = JSON.parse(savedMaster)
          console.log('Найден сохраненный мастер:', currentMaster.value.name)
          
          // Загружаем данные
          await loadData()
          console.log('Данные загружены для мастера:', currentMaster.value.name)
        } catch (err) {
          console.error('Ошибка при загрузке сохраненного мастера:', err)
          localStorage.removeItem('master')
          localStorage.removeItem('masterToken')
          showLoginModal.value = true
        }
      } else {
        console.log('Нет сохраненного мастера, показываем окно входа')
        showLoginModal.value = true
      }
      
      // Загружаем мастеров
      await loadMasters()
      console.log('Мастеров загружено:', masters.value.length)
    })
    
    // Возвращаем все свойства
    return {
      // Состояние
      activeTab,
      
      // Модальные окна
      showLoginModal,
      showNewOrderModal,
      showNewClientModal,
      showNewPartModal,
      showEditClientModal,
      showEditPartModal,
      showUsePartModal,
      showStatusModal,
      showCostModal,
      showOrderDetailsModal,
      
      // Данные
      orders,
      clients,
      masters,
      spareParts,
      currentMaster,
      selectedOrder,
      selectedPart,
      editingClient,
      editingPart,
      
      // Загрузка и ошибки
      loading,
      error,
      
      // Фильтры
      orderFilter,
      filteredOrders,
      lowStockParts,
      totalOrdersAmount,
      totalClientOrders,
      totalPartsQuantity,
      availableMasters,
      
      // Флаги операций
      loggingIn,
      creatingOrder,
      creatingClient,
      creatingPart,
      
      // Формы
      loginForm,
      newOrder,
      newClient,
      newPart,
      statusUpdate,
      costUpdate,
      usePartData,
      
      // UI
      tabs,
      
      // Методы
      login,
      logout,
      loadOrders,
      createOrder,
      createClient,
      createPart,
      editClient,
      updateClient,
      editPart,
      updatePart,
      openUsePartModal,
      usePart,
      viewOrder,
      openStatusModal,
      updateOrderStatus,
      openCostModal,
      updateOrderCost,
      createOrderForClient,
      assignOrderToMaster,
      filterOrders,
      clearFilters,
      switchTab,
      getActiveTabName,
      getStatusText,
      getStatusClass,
      formatDate,
      truncateText,
      getInitials,
      calculateLaborCost,
      calculateTotalCost,
      closeNewOrderModal,
      closeNewClientModal,
      closeNewPartModal
    }
  }
}
</script>

<style scoped>
/* Базовые стили */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
  background: #f5f7fa;
  min-height: 100vh;
  color: #000000;
}

.app {
  min-height: 100vh;
  color: #000000;
}

/* Модальное окно */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  padding: 1rem;
}

.modal {
  background: white;
  border-radius: 10px;
  width: 90%;
  max-width: 500px;
  max-height: 85vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  box-shadow: 0 10px 30px rgba(0,0,0,0.2);
}

.modal.large-modal {
  max-width: 700px;
  max-height: 90vh;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.5rem;
  background: linear-gradient(135deg, #2a5298 0%, #1e3c72 100%);
  color: white;
  flex-shrink: 0;
}

.modal-header h3 {
  color: white;
  font-size: 1.2rem;
  font-weight: 600;
}

.close-btn {
  background: rgba(255,255,255,0.2);
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: white;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s;
}

.close-btn:hover {
  background: rgba(255,255,255,0.3);
}

.modal-content {
  padding: 1.5rem;
  overflow-y: auto;
  flex: 1;
  max-height: calc(85vh - 70px);
  color: #000000;
}

.modal.large-modal .modal-content {
  max-height: calc(90vh - 70px);
}

/* Формы */
.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.3rem;
  font-weight: 600;
  font-size: 0.9rem;
  color: #000000;
}

.form-group input,
.form-group select,
.form-group textarea {
  width: 100%;
  padding: 0.4rem 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.9rem;
  transition: border 0.2s;
  color: #000000;
  background: white;
}

.form-group input:focus,
.form-group select:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #2a5298;
  box-shadow: 0 0 0 2px rgba(42, 82, 152, 0.1);
}

.form-group textarea {
  resize: vertical;
  min-height: 60px;
  max-height: 150px;
}

.form-row {
  display: flex;
  gap: 0.8rem;
  margin-bottom: 0.5rem;
}

.form-row .form-group {
  flex: 1;
  margin-bottom: 0;
}

.checkbox-group {
  margin-top: 0.5rem;
}

.checkbox-group label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: normal;
  cursor: pointer;
  color: #000000;
}

.cost-preview {
  background: #f8f9fa;
  padding: 0.75rem;
  border-radius: 4px;
  font-size: 0.9rem;
  color: #000000;
}

.cost-preview .total-cost {
  margin-top: 0.5rem;
  padding-top: 0.5rem;
  border-top: 1px solid #dee2e6;
  color: #000000;
}

.form-actions {
  margin-top: 1.5rem;
  display: flex;
  gap: 1rem;
}

.cancel-btn,
.submit-btn {
  flex: 1;
  padding: 0.75rem;
  border-radius: 4px;
  cursor: pointer;
  border: none;
  font-weight: 600;
  font-size: 0.9rem;
  transition: all 0.2s;
}

.cancel-btn {
  background: #f8f9fa;
  color: #495057;
  border: 1px solid #dee2e6;
}

.cancel-btn:hover {
  background: #e9ecef;
}

.submit-btn {
  background: linear-gradient(135deg, #2a5298 0%, #1e3c72 100%);
  color: white;
}

.submit-btn:hover {
  background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%);
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(30, 60, 114, 0.3);
}

.submit-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
}

.demo-note {
  color: #666;
  display: block;
  margin-top: 8px;
  font-size: 0.85rem;
}

.error-text {
  color: #dc3545;
  font-weight: bold;
}

.master-preview {
  margin-top: 20px;
  padding: 15px;
  background: #f8f9fa;
  border-radius: 8px;
  color: #000000;
}

.master-preview h4 {
  color: #000000;
  margin-bottom: 10px;
}

.master-preview p {
  color: #000000;
  margin: 5px 0;
}

/* Шапка */
.header {
  background: linear-gradient(135deg, #2a5298 0%, #1e3c72 100%);
  color: white;
  padding: 1rem 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
}

.header-left h1 {
  font-size: 1.5rem;
  font-weight: 700;
  color: white;
}

.header-left p {
  font-size: 0.9rem;
  opacity: 0.9;
  margin-top: 0.2rem;
  color: rgba(255, 255, 255, 0.9);
}

.user-info {
  display: flex;
  align-items: center;
  gap: 1rem;
  color: white;
}

.login-link {
  cursor: pointer;
  color: #a0c8ff;
  font-weight: 600;
  transition: color 0.2s;
}

.login-link:hover {
  color: white;
}

.logout-btn {
  background: rgba(255,255,255,0.2);
  border: 1px solid rgba(255,255,255,0.3);
  color: white;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  transition: all 0.2s;
}

.logout-btn:hover {
  background: rgba(255,255,255,0.3);
  transform: translateY(-1px);
}

/* Приглашение ко входу */
.login-prompt {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: calc(100vh - 70px);
  padding: 2rem;
  text-align: center;
}

.login-content {
  background: white;
  padding: 2rem;
  border-radius: 10px;
  box-shadow: 0 5px 20px rgba(0,0,0,0.1);
  max-width: 400px;
  width: 100%;
}

.login-content h2 {
  margin-bottom: 1rem;
  color: #000000;
}

.login-content p {
  color: #000000;
}

.login-btn {
  background: linear-gradient(135deg, #2a5298 0%, #1e3c72 100%);
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 4px;
  cursor: pointer;
  margin-top: 1.5rem;
  font-weight: 600;
  width: 100%;
  transition: all 0.2s;
}

.login-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(30, 60, 114, 0.4);
}

/* Основной контент */
.main-content {
  display: flex;
  min-height: calc(100vh - 70px);
}

/* Боковое меню */
.sidebar {
  width: 200px;
  background: white;
  border-right: 1px solid #e0e0e0;
  flex-shrink: 0;
}

.sidebar-nav {
  display: flex;
  flex-direction: column;
  padding: 0.5rem;
}

.nav-btn {
  background: none;
  border: none;
  padding: 0.75rem 1rem;
  text-align: left;
  cursor: pointer;
  border-left: 3px solid transparent;
  border-radius: 4px;
  margin-bottom: 0.25rem;
  font-size: 0.95rem;
  color: #495057;
  transition: all 0.2s;
}

.nav-btn:hover {
  background: #f8f9fa;
  color: #2a5298;
}

.nav-btn.active {
  background: #e8f0fe;
  border-left-color: #2a5298;
  font-weight: 600;
  color: #2a5298;
}

/* Рабочая область */
.workspace {
  flex: 1;
  padding: 1.5rem;
  background: #f5f7fa;
  overflow-y: auto;
}

.workspace-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  padding: 1rem 1.5rem;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.08);
}

.workspace-header h2 {
  color: #000000;
  font-size: 1.3rem;
}

.action-buttons {
  display: flex;
  gap: 0.5rem;
}

.add-btn,
.refresh-btn {
  background: linear-gradient(135deg, #2a5298 0%, #1e3c72 100%);
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  font-weight: 600;
  transition: all 0.2s;
}

.add-btn:hover,
.refresh-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(30, 60, 114, 0.3);
}

.refresh-btn {
  background: #6c757d;
}

.refresh-btn:hover {
  background: #5a6268;
}

/* Статистика */
.order-stats {
  display: flex;
  gap: 1rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}

.stat-item {
  background: white;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.1);
  font-size: 0.9rem;
  color: #000000;
}

.client-stats,
.parts-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.stat-card {
  background: white;
  padding: 1rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.08);
  color: #000000;
}

.stat-card h3 {
  font-size: 0.9rem;
  color: #000000;
  margin-bottom: 0.5rem;
}

.stat-value {
  font-size: 1.5rem;
  font-weight: 700;
  color: #000000;
}

/* Состояния загрузки/ошибок */
.loading-state,
.error-state,
.empty-state {
  text-align: center;
  padding: 3rem;
  background: white;
  border-radius: 8px;
  margin: 1rem 0;
  box-shadow: 0 2px 8px rgba(0,0,0,0.08);
  color: #000000;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #f3f3f3;
  border-top: 3px solid #2a5298;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.error-state button,
.empty-state button {
  margin-top: 1rem;
  padding: 0.5rem 1rem;
  background: #2a5298;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

/* Карточки */
.orders-grid,
.clients-grid,
.masters-grid,
.parts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1rem;
  margin-top: 1rem;
}

.order-card,
.client-card,
.master-card,
.part-card {
  background: white;
  border-radius: 8px;
  padding: 1rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.08);
  transition: transform 0.2s, box-shadow 0.2s;
  color: #000000;
}

.order-card:hover,
.client-card:hover,
.master-card:hover,
.part-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(0,0,0,0.1);
}

.order-header,
.client-header,
.master-header,
.part-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
}

.order-number {
  font-weight: 600;
  color: #000000;
}

.urgent-badge {
  background: #dc3545;
  color: white;
  padding: 0.2rem 0.5rem;
  border-radius: 3px;
  font-size: 0.7rem;
  margin-left: 0.5rem;
}

/* Бейджи статусов */
.status-badge {
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: white;
}

.status-badge.new { background: #e3f2fd; color: #1976d2; }
.status-badge.accepted { background: #fff3e0; color: #f57c00; }
.status-badge.in-progress { background: #fce4ec; color: #c2185b; }
.status-badge.waiting-parts { background: #e8eaf6; color: #3949ab; }
.status-badge.repair { background: #f3e5f5; color: #7b1fa2; }
.status-badge.ready { background: #e8f5e8; color: #2e7d32; }
.status-badge.completed { background: #e0f2f1; color: #00796b; }
.status-badge.cancelled { background: #ffebee; color: #d32f2f; }

.status-indicator {
  font-size: 0.8rem;
  padding: 0.2rem 0.5rem;
  border-radius: 3px;
}

.status-indicator.active {
  background: #d4edda;
  color: #155724;
}

.status-indicator.inactive {
  background: #f8d7da;
  color: #721c24;
}

/* Содержимое карточек */
.order-body,
.client-info,
.master-info,
.part-info {
  margin-bottom: 1rem;
  color: #000000;
}

.order-body h4 {
  margin-bottom: 0.5rem;
  color: #000000;
}

.client-phone {
  color: #000000;
  font-size: 0.9rem;
}

.device-info p {
  margin-bottom: 0.5rem;
  font-size: 0.9rem;
  color: #000000;
}

.order-meta {
  background: #f8f9fa;
  padding: 0.75rem;
  border-radius: 4px;
  margin: 1rem 0;
  color: #000000;
}

.meta-item {
  display: flex;
  justify-content: space-between;
  margin-bottom: 0.5rem;
  font-size: 0.85rem;
  color: #000000;
}

.meta-item:last-child {
  margin-bottom: 0;
}

.meta-item .label {
  color: #000000;
}

.price {
  font-weight: 600;
  color: #000000;
}

/* Кнопки действий */
.order-actions,
.client-actions,
.part-actions,
.master-actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 1rem;
}

.action-btn {
  flex: 1;
  padding: 0.5rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.3rem;
  transition: all 0.2s;
  color: white;
}

.action-btn.primary {
  background: #2a5298;
}

.action-btn.primary:hover {
  background: #1e3c72;
  transform: translateY(-1px);
}

.action-btn.secondary {
  background: #6c757d;
}

.action-btn.secondary:hover {
  background: #5a6268;
  transform: translateY(-1px);
}

.action-btn.success {
  background: #28a745;
}

.action-btn.success:hover {
  background: #218838;
  transform: translateY(-1px);
}

/* Запчасти */
.sku {
  background: #f8f9fa;
  color: #495057;
  padding: 0.2rem 0.5rem;
  border-radius: 3px;
  font-size: 0.8rem;
  font-family: monospace;
}

.part-stock {
  margin: 1rem 0;
  color: #000000;
}

.stock-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.quantity {
  font-weight: 600;
}

.quantity.critical {
  color: #dc3545;
}

.quantity.normal {
  color: #28a745;
}

.stock-min {
  font-size: 0.85rem;
  color: #000000;
}

.part-card.low-stock {
  border: 1px solid #dc3545;
  box-shadow: 0 0 0 1px rgba(220, 53, 69, 0.1);
}

/* Профиль мастера */
.master-profile {
  background: white;
  border-radius: 8px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.08);
  color: #000000;
}

.profile-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.master-avatar {
  width: 60px;
  height: 60px;
  background: linear-gradient(135deg, #2a5298 0%, #1e3c72 100%);
  color: white;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  font-weight: 600;
  flex-shrink: 0;
}

.profile-info h3 {
  margin-bottom: 0.25rem;
  color: #000000;
}

.specialization {
  color: #000000;
  font-size: 0.9rem;
  margin-bottom: 0.75rem;
}

.profile-stats {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}

.profile-stats span {
  background: #f8f9fa;
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.85rem;
  color: #000000;
}

.master-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.master-title {
  flex: 1;
  color: #000000;
}

.master-rating {
  background: #ffc107;
  color: #212529;
  padding: 0.25rem 0.5rem;
  border-radius: 3px;
  font-weight: 600;
  font-size: 0.9rem;
}

/* Детали заказа */
.order-details {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  color: #000000;
}

.detail-section {
  background: #f8f9fa;
  padding: 1rem;
  border-radius: 6px;
  color: #000000;
}

.detail-section h4 {
  margin-bottom: 0.75rem;
  color: #000000;
  font-size: 1rem;
}

.detail-row {
  display: flex;
  margin-bottom: 0.5rem;
  font-size: 0.9rem;
  color: #000000;
}

.detail-row:last-child {
  margin-bottom: 0;
}

.detail-row .label {
  width: 180px;
  color: #000000;
  font-weight: 500;
  flex-shrink: 0;
}

.modal-actions {
  margin-top: 1.5rem;
  display: flex;
  gap: 0.5rem;
}

/* Адаптивность */
@media (max-width: 768px) {
  .main-content {
    flex-direction: column;
  }
  
  .sidebar {
    width: 100%;
    border-right: none;
    border-bottom: 1px solid #e0e0e0;
  }
  
  .sidebar-nav {
    flex-direction: row;
    overflow-x: auto;
    padding: 0.5rem 0.25rem;
  }
  
  .nav-btn {
    white-space: nowrap;
    border-left: none;
    border-bottom: 3px solid transparent;
    margin-bottom: 0;
    margin-right: 0.25rem;
  }
  
  .nav-btn.active {
    border-left: none;
    border-bottom-color: #2a5298;
  }
  
  .workspace-header {
    flex-direction: column;
    align-items: stretch;
    gap: 1rem;
  }
  
  .action-buttons {
    width: 100%;
  }
  
  .add-btn,
  .refresh-btn {
    flex: 1;
    justify-content: center;
  }
  
  .orders-grid,
  .clients-grid,
  .masters-grid,
  .parts-grid {
    grid-template-columns: 1fr;
  }
  
  .form-row {
    flex-direction: column;
    gap: 0;
  }
  
  .client-stats,
  .parts-stats {
    grid-template-columns: 1fr;
  }
  
  .modal {
    max-width: 95%;
  }
  
  .modal-header {
    padding: 0.75rem 1rem;
  }
  
  .modal-content {
    padding: 1rem;
  }
}

/* Фильтры */
.filters {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}

.filters select {
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  background: white;
  min-width: 150px;
  color: #000000;
}
</style>