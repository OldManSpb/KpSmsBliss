KpSmsBliss
================
 
Драйвер KpSmsBliss.dll является прикладной библиотекой для приложения Scada Communicator проекта RapidScada. При помощи данного драйвера можно осуществлять отправку СМС сообщений через сервис smsbliss.ru.

Настройка драйвера
-------------------

В основном настройка ничем не отличается от установки и настройки любого драйвера коммкникатора. Окно комфигурации содtржит следующие пункты
### Адрес REST API
Адрес скрипта, вызываемого при отправке СМС
### Пользователь
Логин в системе smsbliss.ru
### Пароль
Пароль в системе smsbliss.ru
### Имя отправителя
Имя, от которого будут приходить СМС. Имя должно быть зарегистрировано в сервисе, иначе сервис выдаст ошибку

Отправка сообщений
--------------------
Для отправки сообщения необходимо передать команду ТУ как бинарную команду, в виде строки. Строка должна содержать две части - телефон (или группа адресной книги), и текст сообщения, разделенные точкой с запятой.
### Примеры
+71234567890;Текст

Group1;Текст

Автоматическая отправка сообщений
-------------------------
Автоматическая отправка сообщений по различным событиям и условиям производится посредством модуля автоматичекого управления.

KpSmsBliss
================
 
Driver KpSmsBliss.dll is a library for Scada Communicator of RapidScada project. With this library it is able to send SMS via service smsbliss.ru.

Driver config
-------------------

Configuration of the driver is similar to any communicator dll.
### Адрес REST API
Address of REST API script
### Пользователь
smsbliss.ru login
### Пароль
smsbliss.ru password
### Имя отправителя
Sender`s name. Be sure, you have registered this name in service.

Send messages
--------------------
To send message send binary string command of the following format:
### Examples
+71234567890;Текст

Group1;Текст

Send messages automatically
-------------------------
Automatic messages can be sent by automatic control module 

