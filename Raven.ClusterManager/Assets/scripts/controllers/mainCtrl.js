/// <reference path="../main.js" />
/*global angular:false */
'use strict';

clusterManagerApp.controller('MainCtrl', function mainCtrl($scope, $http, $timeout) {
    $scope.startDiscovering = function () {
        $scope.isDiscovering = true;
        $http.get('/api/discovery/start').success(function () { //calls to MVC controller by http get
            $scope.isDiscovering = false;
        });
    };

    var timeoutPromise;
    $scope.getStats = function () {
        $http.get('/api/servers').success(function (result) {
            $scope.stats = result;
            _.forEach($scope.stats.servers, function (value, index, array) {
                value.cssClass = '';
                if (value.isOnline) {
                    if (value.isUnauthorized) {
                        value.cssClass += ' warning';
                    } else {
                        value.cssClass += ' success';
                    }
                } else {
                    value.cssClass += ' error';
                }
              
            });
        });
        
        // timeoutPromise = $timeout($scope.getStats, 5000);
    };
    $scope.getStats();
    $scope.getNewKey = function () {
        var newApiKey = "";
        var possible = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        for (var i = 0; i < 22; i++)
            newApiKey += possible.charAt(Math.floor(Math.random() * possible.length));

        return newApiKey;

    };

    $scope.generateSecret = function () {

        $scope.Secret = $scope.getNewKey();
    };

    $scope.doesNotHaveName = function () {
        return !$scope.name;
    };


    $scope.saveApiKeys = function () {

        $scope.FullApiKey = $scope.name + '/' + $scope.Secret;

        var apiKeysToSave = []; // Array of class ApiKeyViewModel
        _.forEach($scope.stats.servers, function (value, index, array) {
            if (value.serverCheckbox == false)
                return;

            apiKeysToSave.push({ Secret: value.Secret, Name: value.name, Server: value.Id });// If checked then get info
        });

        $http.post('/api/servers/save-api', apiKeysToSave)
            .success(function (result) {
                alert('Done');
            });
      //  $http.post('/api/servers/createNewServer', serverToSave)
        //  .success(function (result) {
          //    alert('Done');
          //});
    };
});